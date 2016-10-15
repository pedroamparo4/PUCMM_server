using System;
using System.Net;
using System.Net.Sockets;

namespace server.SERVER_CORE
{
    public class HTTPServer : IDisposable
    {
       
        private HTTPServerState.STATE _state;
        private IPEndPoint _endPoint;
        private TcpListener _listener;
        private int _port;
        private bool _disposed = false;

        public HTTPServer(int port)
        {
            this.Port = port;
            this._endPoint = new IPEndPoint(IPAddress.Loopback, this.Port);
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

        public void Dispose()
        {
            _disposed = true;
        }

    }
}
