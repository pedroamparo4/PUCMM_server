using System;
using System.Web;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;
using server.SERVER_CORE.ErrorHandling;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;
using System.Globalization;

namespace server.SERVER_CORE
{
    internal class HTTPClient : IDisposable
    {
        // Class private members
        private ClientState.STATE _state = ClientState.STATE.CLOSED;
        private bool _disposed = false;
        private static readonly Regex PrologRegex = new Regex("^([A-Z]+) ([^ ]+) (HTTP/[^ ]+)$", RegexOptions.Compiled);
        private readonly byte[] _writeBuffer;
        private NetworkStream _stream;
        private MemoryStream _writeStream;
        private bool _errored = false;
        private HttpRequestParser _parser;
        private HttpContext _context;

        // Members used by Properties
        private TcpClient _tcpClient;
        private HTTPServer _server;

        #region Constructors

        public HTTPClient(HTTPServer httpServer, TcpClient tcpClient)
        {
            if (httpServer == null)
                throw new ArgumentNullException(nameof(httpServer));
            if (tcpClient == null)
                throw new ArgumentNullException(nameof(tcpClient));

            Server = httpServer;
            TcpClient = tcpClient;

            ReadBuffer = new HttpReadBuffer(httpServer.ReadBufferSize);
            _writeBuffer = new byte[httpServer.WriteBufferSize];

            _stream = tcpClient.GetStream();
        }

        #endregion

        #region Public Methods

        public void BeginRequest()
        {
            //Console.WriteLine("HttpClient.BeginRequest() method has been called.");
            Reset();
            BeginRead();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Server.UnregisterClient(this);
                _state = ClientState.STATE.CLOSED;
                if (_stream != null)
                {
                    _stream.Dispose();
                    _stream = null;
                }
                if (TcpClient != null)
                {
                    TcpClient.Close();
                    TcpClient = null;
                }
                Reset();
                _disposed = true;
            }
        }

        public void RequestClose()
        {
            if (_state == ClientState.STATE.READING_PROLOG)
            {
                var stream = _stream;
                if (stream != null)
                    stream.Dispose();
            }
        }

        public void ForceClose()
        {
            var stream = _stream;
            if (stream != null)
                stream.Dispose();
        }

        public void UnsetParser()
        {
            Debug.Assert(_parser != null);
            _parser = null;
        }

        #endregion

        #region Private Methods

        private void BeginRead()
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                // Reads should be within a certain timeframe ////

                Server.TimeoutManager.ReadQueue.Add(
                    ReadBuffer.BeginRead(_stream, ReadCallback, null), //null obj
                    this
                );
            }
            catch (Exception ex)
            {
                Dispose();
                ProcessException(ex);
            }
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                return;
            }
            if (_state == ClientState.STATE.READING_PROLOG && Server.State != HTTPServerState.STATE.STARTED)
            {
                Dispose();
                return;
            }

            try
            {
                ReadBuffer.EndRead(_stream, asyncResult);
                if (ReadBuffer.DataAvailable)
                    ProcessReadBuffer();
                else
                    Dispose();
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }
        }

        private void ProcessException(Exception exception)
        {
            if (_disposed)
                return;
            _errored = true;

            // If there is no request available, the error didn't occur as part
            // of a request (e.g. the client closed the connection). Just close
            // the channel in that case.
            if (Request == null)
            {
                Dispose();
                return;
            }

            try
            {
                if (_context == null)
                    _context = new HttpContext(this);

                _context.Response.Status = "500 Internal Server Error";

                bool handled;
                try
                {
                    handled = Server.RaiseUnhandledException(_context, exception);
                }
                catch
                {
                    handled = false;
                }

                if (!handled && _context.Response.OutputStream.CanWrite)
                {
                    string resourceName = GetType().Namespace + ".Resources.InternalServerError.html";
                    using (var stream = GetType().Assembly.GetManifestResourceStream(resourceName))
                    {
                        byte[] buffer = new byte[4096];
                        int read;
                        while ((read = stream.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            _context.Response.OutputStream.Write(buffer, 0, read);
                        }
                    }
                }

                WriteResponseHeaders();
            }
            catch (Exception /*ex*/)
            {
                Dispose();
            }
        }

        private void ProcessReadBuffer()
        {
            while (ReadBuffer.DataAvailable && _writeStream == null)
            {
                switch (_state)
                {
                    case ClientState.STATE.READING_PROLOG:
                        ProcessProlog();
                        break;
                    case ClientState.STATE.READING_HEADERS:
                        ProcessHeaders();
                        break;
                    case ClientState.STATE.READING_CONTENT:
                        ProcessContent();
                        break;
                    default:
                        throw new InvalidOperationException("Invalid Client State: " + _state.ToString());
                }
            }
            if (_writeStream == null)
            {
                BeginRead();
            }
        }

        private void ProcessProlog()
        {
            var line = ReadBuffer.ReadLine();
            if (line == null)
            {
                return;
            }
            var match = PrologRegex.Match(line);
            if (!match.Success)
            {
                throw new ProtocolException("The following line could not be parsed by PrologRegex member: " + line);
            }

            Method = match.Groups[1].Value;
            Request = match.Groups[2].Value;
            Protocol = match.Groups[3].Value;

            Console.WriteLine("Method: " + Method); ////////
            Console.WriteLine("Request: " + Request); ////////
            Console.WriteLine("Protocol: " + Protocol); ////////

            _state = ClientState.STATE.READING_HEADERS;
            ProcessHeaders();
        }

        private void ProcessHeaders()
        {
            string line;
            while ((line = ReadBuffer.ReadLine()) != null)
            {
                if (line.Length == 0)
                {
                    ReadBuffer.Reset();
                    _state = ClientState.STATE.READING_CONTENT;
                    ProcessContent();
                    return;
                }
                string[] parts = line.Split(new[] { ':' }, 2);
                if (parts.Length != 2)
                {
                    throw new ProtocolException("Received header without colon.");
                }
                Headers[parts[0].Trim()] = parts[1].Trim();
            }
        }

        private void ProcessContent()
        {
            if (_parser != null)
            {
                _parser.Parse();
                return;
            }

            if (ProcessExpectHeader())
                return;

            if (ProcessContentLengthHeader())
                return;

            ExecuteRequest();
        }

        internal void ExecuteRequest() ////////made internal, originally private
        {
            _context = new HttpContext(this);
            Server.RaiseRequest(_context);
            WriteResponseHeaders();
        }

        private void WriteResponseHeaders()
        {
            byte[] headers = BuildResponseHeaders();
            if (_writeStream != null)
                _writeStream.Dispose();
            _writeStream = new MemoryStream(headers);
            _state = ClientState.STATE.WRITING_HEADERS;
            BeginWrite();
        }

        private void Reset()
        {
            _state = ClientState.STATE.READING_PROLOG;
            _context = null;
            if (_parser != null)
            {
                _parser.Dispose();
                _parser = null;
            }

            if (_writeStream != null)
            {
                _writeStream.Dispose();
                _writeStream = null;
            }

            if (InputStream != null)
            {
                InputStream.Dispose();
                InputStream = null;
            }

            ReadBuffer.Reset();
            Method = null;
            Protocol = null;
            Request = null;
            Headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            PostParameters = new NameValueCollection();

            if (MultiPartItems != null)
            {
                foreach (var item in MultiPartItems)
                {
                    if (item.Stream != null)
                        item.Stream.Dispose();
                }

                MultiPartItems = null;
            }
        }

        private bool ProcessExpectHeader()
        {
            // Process the Expect: 100-continue header.
            string expectHeader;
            if (Headers.TryGetValue("Expect", out expectHeader))
            {
                // Remove the expect header for the next run.
                Headers.Remove("Expect");
                int pos = expectHeader.IndexOf(';');
                if (pos != -1)
                    expectHeader = expectHeader.Substring(0, pos).Trim();
                if (!String.Equals("100-continue", expectHeader, StringComparison.OrdinalIgnoreCase))
                    throw new ProtocolException(String.Format("Could not process Expect header '{0}'", expectHeader));
                SendContinueResponse();
                return true;
            }
            return false;
        }

        private bool ProcessContentLengthHeader()
        {
            // Read the content.
            string contentLengthHeader;
            if (Headers.TryGetValue("Content-Length", out contentLengthHeader))
            {
                int contentLength;
                if (!int.TryParse(contentLengthHeader, out contentLength))
                    throw new ProtocolException(String.Format("Could not parse Content-Length header '{0}'", contentLengthHeader));
                string contentTypeHeader;
                string contentType = null;
                string contentTypeExtra = null;
                if (Headers.TryGetValue("Content-Type", out contentTypeHeader))
                {
                    string[] parts = contentTypeHeader.Split(new[] { ';' }, 2);
                    contentType = parts[0].Trim().ToLowerInvariant();
                    contentTypeExtra = parts.Length == 2 ? parts[1].Trim() : null;
                }
                if (_parser != null)
                {
                    _parser.Dispose();
                    _parser = null;
                }
                switch (contentType)
                {
                    case "application/x-www-form-urlencoded":
                        _parser = new HttpUrlEncodedRequestParser(this, contentLength);
                        break;
                    case "multipart/form-data":
                        string boundary = null;
                        if (contentTypeExtra != null)
                        {
                            string[] parts = contentTypeExtra.Split(new[] { '=' }, 2);
                            if (
                                parts.Length == 2 &&
                                String.Equals(parts[0], "boundary", StringComparison.OrdinalIgnoreCase)
                            )
                                boundary = parts[1];
                        }
                        if (boundary == null)
                            throw new ProtocolException("Expected boundary with multipart content type");
                        _parser = new HttpMultiPartRequestParser(this, contentLength, boundary);
                        break;
                    default:
                        _parser = new HttpUnknownRequestParser(this, contentLength);
                        break;
                }
                // We've made a parser available. Recurs back to start processing
                // with the parser.
                ProcessContent();
                return true;
            }
            return false;
        }

        private void SendContinueResponse()
        {
            var sb = new StringBuilder();
            sb.Append(Protocol);
            sb.Append(" 100 Continue\r\nServer: ");
            sb.Append(Server.ServerBanner);
            sb.Append("\r\nDate: ");
            sb.Append(DateTime.UtcNow.ToString("R"));
            sb.Append("\r\n\r\n");
            var bytes = Encoding.ASCII.GetBytes(sb.ToString());
            if (_writeStream != null)
                _writeStream.Dispose();
            _writeStream = new MemoryStream();
            _writeStream.Write(bytes, 0, bytes.Length);
            _writeStream.Position = 0;
            BeginWrite();
        }

        private void ProcessRequestCompleted()
        {
            string connectionHeader = string.Empty;
            // Do not accept new requests when the server is stopping.
            if (
                !_errored &&
                Server.State == HTTPServerState.STATE.STARTED &&
                Headers.TryGetValue("Connection", out connectionHeader) &&
                String.Equals(connectionHeader, "keep-alive", StringComparison.OrdinalIgnoreCase)
            )
                BeginRequest();
            else
                Dispose();
        }

        private byte[] BuildResponseHeaders()
        {
            var response = _context.Response;
            var sb = new StringBuilder();

            // Write the prolog.
            sb.Append(Protocol);
            sb.Append(' ');
            sb.Append(response.StatusCode);
            if (!String.IsNullOrEmpty(response.StatusDescription))
            {
                sb.Append(' ');
                sb.Append(response.StatusDescription);
            }
            sb.Append("\r\n");

            // Write all headers provided by Response.
            if (!String.IsNullOrEmpty(response.CacheControl))
                WriteHeader(sb, "Cache-Control", response.CacheControl);

            if (!String.IsNullOrEmpty(response.ContentType))
            {
                string contentType = response.ContentType;
                if (!String.IsNullOrEmpty(response.CharSet))
                    contentType += "; charset=" + response.CharSet;
                WriteHeader(sb, "Content-Type", contentType);
            }

            WriteHeader(sb, "Expires", response.ExpiresAbsolute.ToString("R"));

            if (!String.IsNullOrEmpty(response.RedirectLocation))
                WriteHeader(sb, "Location", response.RedirectLocation);

            // Write the remainder of the headers.
            foreach (string key in response.Headers.AllKeys)
            {
                WriteHeader(sb, key, response.Headers[key]);
            }

            // Write the content length (we override custom headers for this).
            WriteHeader(sb, "Content-Length", response.OutputStream.BaseStream.Length.ToString(CultureInfo.InvariantCulture));
            for (int i = 0; i < response.Cookies.Count; i++)
            {
                WriteHeader(sb, "Set-Cookie", response.Cookies[i].GetHeaderValue());
            }
            sb.Append("\r\n");

            return response.HeadersEncoding.GetBytes(sb.ToString());
        }

        private void WriteHeader(StringBuilder sb, string key, string value)
        {
            sb.Append(key);
            sb.Append(": ");
            sb.Append(value);
            sb.Append("\r\n");
        }

        private void WriteResponseContent()
        {
            if (_writeStream != null)
                _writeStream.Dispose();
            _writeStream = _context.Response.OutputStream.BaseStream;
            _writeStream.Position = 0;
            _state = ClientState.STATE.WRITING_CONTENT;
            BeginWrite();
        }

        private void BeginWrite()
        {
            try
            {
                int read = _writeStream.Read(_writeBuffer, 0, _writeBuffer.Length);
                Server.TimeoutManager.WriteQueue.Add(
                    _stream.BeginWrite(_writeBuffer, 0, read, WriteCallback, null),
                    this
                );
            }
            catch (Exception ex)
            {
                Dispose();
                ProcessException(ex);
            }
        }

        private void WriteCallback(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                return;
            }
            try
            {
                _stream.EndWrite(asyncResult);
                if (_writeStream != null && _writeStream.Length != _writeStream.Position)
                {
                    BeginWrite();
                }
                else
                {
                    if (_writeStream != null)
                    {
                        _writeStream.Dispose();
                        _writeStream = null;
                    }
                    switch (_state)
                    {
                        case ClientState.STATE.WRITING_HEADERS:
                            WriteResponseContent();
                            break;
                        case ClientState.STATE.WRITING_CONTENT:
                            ProcessRequestCompleted();
                            break;
                        default:
                            Debug.Assert(_state != ClientState.STATE.CLOSED);
                            if (ReadBuffer.DataAvailable)
                            {
                                try
                                {
                                    ProcessReadBuffer();
                                }
                                catch (Exception ex)
                                {
                                    ProcessException(ex);
                                }
                            }
                            else
                            {
                                BeginRead();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Something happened! {ex.Message}");
                Dispose();
                ProcessException(ex);
            }
        }


        #endregion

        #region Properties

        public HTTPServer Server
        {
            get { return _server; }
            private set { _server = value; }
        }

        public TcpClient TcpClient
        {
            get { return _tcpClient; }
            private set { _tcpClient = value; }
        }

        public HttpReadBuffer ReadBuffer { get; private set; }

        public Stream InputStream { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        public string Method { get; private set; }

        public string Protocol { get; private set; }

        public string Request { get; private set; }

        public List<HttpMultiPartItem> MultiPartItems { get; set; }

        public NameValueCollection PostParameters { get; set; }

        #endregion

    }
}
