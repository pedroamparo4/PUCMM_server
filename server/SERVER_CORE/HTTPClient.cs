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

namespace server.SERVER_CORE
{
    public class HTTPClient : IDisposable
    {
        private ClientState.STATE _state = ClientState.STATE.CLOSED;
        private bool _disposed = false;
        private static readonly Regex PrologRegex = new Regex("^([A-Z]+) ([^ ]+) (HTTP/[^ ]+)$", RegexOptions.Compiled);
        private readonly byte[] _writeBuffer;
        private NetworkStream _stream;
        private MemoryStream _writeStream;
        //////private bool _errored = false;
        private HttpRequestParser _parser;

        private TcpClient _tcpClient;
        private HTTPServer _server;

        #region Constructors

        public HTTPClient(HTTPServer httpServer, TcpClient tcpClient, int readBufferSize, int writeBufferSize)
        {
            if (httpServer == null)
            { throw new ArgumentNullException("HttpServer argument provided is null."); }
            Server = httpServer;

            if (tcpClient == null)
            { throw new ArgumentNullException("TcpClient argument provided is null."); }
            TcpClient = tcpClient;

            if (readBufferSize < 0)
            { throw new ArgumentOutOfRangeException("ReadBufferSize argument provided is a negative number."); }
            ReadBuffer = new HttpReadBuffer(readBufferSize);

            if (writeBufferSize < 0)
            { throw new ArgumentOutOfRangeException("WriteBufferSize argument provided is a negative number."); }
            _writeBuffer = new byte[writeBufferSize];

            _stream = TcpClient.GetStream();

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
                ////// Server.UnregisterClient(this);
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
            catch (Exception /*ex*/) ////
            {
                Dispose();
            }
        }

        private void ReadCallback(IAsyncResult asyncResult)
        {
            if (_disposed)
            {
                return;
            }
            if (_state == ClientState.STATE.READING_PROLOG && Server.State != SERVER_CORE.HTTPServerState.STATE.STARTED)
            {
                Dispose();
                return;
            }

            try
            {
                ReadBuffer.EndRead(_stream, asyncResult);
            }
            catch (ObjectDisposedException)
            {
                Dispose();
            }
            catch (Exception ex)
            {
                ProcessException(ex);
            }

            if (ReadBuffer.DataAvailable)
            {
                ProcessReadBuffer();
            }
            else
            {
                Dispose();
            }
        }

        private void ProcessException(Exception ex) { }

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

            Console.WriteLine("Method: " + Method);
            Console.WriteLine("Request: " + Request);
            Console.WriteLine("Protocol: " + Protocol);

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
                string[] parts = line.Split(':');
                if (parts.Length < 2)
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
            else if (ProcessExpectHeader())
            {
                return;

            }
            else if (ProcessContentLengthHeader())
            {
                return;
            }
            else
            {
                ExecuteRequest();
            }
        }

        internal void ExecuteRequest()
        {
            Console.WriteLine("REQUEST METHOD HAS BEEN CALLED!");
        }

        private void Reset()
        {
            _state = ClientState.STATE.READING_PROLOG;
            //_context = null;
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
            //BeginWrite();
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

        internal HttpReadBuffer ReadBuffer { get; private set; }

        public Stream InputStream { get; set; }

        public Dictionary<string, string> Headers { get; private set; }

        public string Method { get; private set; }

        public string Protocol { get; private set; }

        public string Request { get; private set; }

        internal List<HttpMultiPartItem> MultiPartItems { get; set; }

        public NameValueCollection PostParameters { get; set; }

        #endregion

    }
}
