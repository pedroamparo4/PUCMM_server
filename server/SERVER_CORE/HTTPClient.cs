using System;
using System.Net.Sockets;
using System.Text.RegularExpressions;

namespace server.SERVER_CORE
{
    public class HTTPClient : IDisposable
    {
        private ClientState.STATE _state;
        private HTTPServer _server;
        private TcpClient _tcpClient;
        private bool _disposed = false;
        private static readonly Regex PrologRegex = new Regex("^([A-Z]+) ([^ ]+) (HTTP/[^ ]+)$", RegexOptions.Compiled);

        public HTTPClient(HTTPServer server, TcpClient tcpClient)
        {
            if (server != null)
            {
                this.Server = server;
            }
            else
            {
                throw new ArgumentNullException("server", "HttpServer provided is null");
            }

            if (tcpClient != null)
            {
                this.TcpClient = tcpClient;
            }
            else
            {
                throw new ArgumentNullException("tcpClient", "TcpClient provided is null");
            }
            
        }

        public void BeginRequest() { }

        public void Dispose()
        {
            this._disposed = true;
        }

        public HTTPServer Server
        {
            get { return this._server; }
            private set { this._server = value; }
        }

        public TcpClient TcpClient
        {
            get { return this._tcpClient; }
            private set { this._tcpClient = value; }
        }
    }
}
