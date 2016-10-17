using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

namespace server.SERVER_CORE
{
    public class HTTPServer : IDisposable
    {
       
        private HTTPServerState.STATE _state;
        private int _readBufferSize;
        private int _writeBufferSize;
        private string _serverBanner;
        private TimeSpan _readTimeout;
        private TimeSpan _writeTimeout;
        private TimeSpan _shutdownTimeout;
        private IPEndPoint _endPoint;
        private TcpListener _listener;
        private int _port;
        private bool _disposed = false;
        private object _syncLock = new object();
        private Dictionary<HTTPClient, bool> _clients = new Dictionary<HTTPClient, bool>();


        public HTTPServer(int port)
        {
            this.Port = port;
            this._endPoint = new IPEndPoint(IPAddress.Loopback, this.Port);

            ReadBufferSize = CORE._ReadBufferSize;
            WriteBufferSize = CORE._WriteBufferSize;
            ServerBanner = String.Format($"{CORE._server_banner_name}/{GetType().Assembly.GetName().Version}");
            ReadTimeout = new TimeSpan(0, 1, 30);
            WriteTimeout = new TimeSpan(0, 1, 30);
            ShutdownTimeout = new TimeSpan(0, 0, 30);

        }

        private void BeginAcceptTcpClient()
        { }

        private void AcceptTcpClientCallback(IAsyncResult iar)
        { }

        private void RegisterClient(HTTPClient client)
        { }

        private void VerifyState(HTTPServerState.STATE state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (_state != state)
                throw new InvalidOperationException($"SERVER SHOULD BE ON STATE: [{state}]");
        }

        public void Start()
        {
            this._state = HTTPServerState.STATE.STARTING;
            this._listener = new TcpListener(EndPoint);
            this._listener.Start();
            this.EndPoint = this._listener.LocalEndpoint as IPEndPoint;
            this._state = HTTPServerState.STATE.STARTED;
        }

        public void Stop()
        {
            this._state = HTTPServerState.STATE.STOPPING;
            this._listener.Stop();
            this._listener = null;
            this._state = HTTPServerState.STATE.STOPPED;
        }
        
        public int Port
        {
            get { return this._port; }
            private set { this._port = value; }
        }

        public IPEndPoint EndPoint
        {
            get { return this._endPoint; }
            private set { this._endPoint = value; }
        }

        public int ReadBufferSize
        {
            get { return this._readBufferSize; }
            set { this._readBufferSize = value; }
        }

        public int WriteBufferSize
        {
            get { return this._writeBufferSize; }
            set { this._writeBufferSize = value; }
        }

        public string ServerBanner
        {
            get { return this._serverBanner; }
            set { this._serverBanner = value; }
        }

        public TimeSpan ReadTimeout
        {
            get { return this._readTimeout; }
            set { this._readTimeout = value; }
        }

        public TimeSpan WriteTimeout
        {
            get { return this._writeTimeout; }
            set { this._writeTimeout = value; }
        }

        public TimeSpan ShutdownTimeout
        {
            get { return this._shutdownTimeout; }
            set { this._shutdownTimeout = value; }
        }

        public void Dispose()
        {
            _disposed = true;
        }

    }
}
