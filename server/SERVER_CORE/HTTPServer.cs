using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using System.Web;

namespace server.SERVER_CORE
{
    public class HTTPServer : IDisposable
    {

        // Class private members
        private TcpListener _listener;
        private bool _disposed = false;
        private object _syncLock = new object();
        private Dictionary<HTTPClient, bool> _clients = new Dictionary<HTTPClient, bool>();
        private AutoResetEvent _clientsChangedEvent = new AutoResetEvent(false);

        // Members used by Properties
        private HTTPServerState.STATE _state = HTTPServerState.STATE.STOPPED;

        #region Constructors

        public HTTPServer(int port)
        {
            if (IsPortAvailable(port))
            { Port = port; }
            else
            {
                Console.WriteLine("ERROR: Port {0} Not Available.", port);
                Port = 8081;  //Default port number
                Console.WriteLine("Using Port {0} instead.", Port);
            }

            EndPoint = new IPEndPoint(IPAddress.Loopback, Port);
            ReadBufferSize = 4096;
            WriteBufferSize = 4096;
            ServerBanner = String.Format("PUCMM_HTTP/{0}", GetType().Assembly.GetName().Version);
            ReadTimeout = TimeSpan.FromSeconds(90);
            WriteTimeout = TimeSpan.FromSeconds(90);
            ShutdownTimeout = TimeSpan.FromSeconds(30);
        }

        #endregion

        #region Public Methods

        public void Start()
        {
            VerifyState(HTTPServerState.STATE.STOPPED);
            TimeoutManager = new HttpTimeoutManager(this);
            _listener = new TcpListener(EndPoint);
            ServerUtility = new HttpServerUtility();
            State = HTTPServerState.STATE.STARTING;
            try
            {
                _listener.Start();
                EndPoint = _listener.LocalEndpoint as IPEndPoint;
                State = HTTPServerState.STATE.STARTED;
                BeginAcceptTcpClient();
            }
            catch
            {
                Console.WriteLine("The Server failed to start.");
                State = HTTPServerState.STATE.STOPPED;
            }
        }

        public void Stop()
        {
            State = HTTPServerState.STATE.STOPPING;
            try
            {
                _listener.Stop();
            }
            catch
            {
                Console.WriteLine("The Server failed to stop.");
            }
            finally
            {
                _listener = null;
                State = HTTPServerState.STATE.STOPPED;
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                if (_state == HTTPServerState.STATE.STARTED)
                    Stop();
                if (_clientsChangedEvent != null)
                {
                    ((IDisposable)_clientsChangedEvent).Dispose();
                    _clientsChangedEvent = null;
                }
                _disposed = true;
            }

            if (TimeoutManager != null)
            {
                TimeoutManager.Dispose();
                TimeoutManager = null;
            }
        }

        #endregion

        #region Private Methods

        private void BeginAcceptTcpClient()
        {
            var listener = _listener;
            if (listener == null)
            {
                throw new NullReferenceException("Local listener from HttpServer.BeginAcceptTcpClient() is null.");
            }
            listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
        }

        private void AcceptTcpClientCallback(IAsyncResult asyncResult)
        {
            try
            {
                var listener = _listener;
                if (listener == null) { return; }
                var tcpClient = listener.EndAcceptTcpClient(asyncResult);
                if (State == HTTPServerState.STATE.STOPPED) { tcpClient.Close(); }
                var client = new HTTPClient(this, tcpClient, ReadBufferSize, WriteBufferSize);
                RegisterClient(client);
                client.BeginRequest();
                ////////listener.BeginAcceptTcpClient(AcceptTcpClientCallback, listener);
                BeginAcceptTcpClient();
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); }
        }

        private void RegisterClient(HTTPClient client)
        {
            if (client == null)
            { throw new ArgumentNullException("HttpClient argument provided is null."); }
            lock (_syncLock)
            {
                _clients.Add(client, false);
                _clientsChangedEvent.Set();
            }
        }

        private void VerifyState(HTTPServerState.STATE state)
        {
            if (_disposed)
                throw new ObjectDisposedException(GetType().Name);
            if (_state != state)
                throw new InvalidOperationException(String.Format("Expected server to be in the '{0}' state", state));
        }

        private bool IsPortAvailable(int port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            foreach (TcpConnectionInformation tcpi in tcpConnInfoArray)
            {
                if (tcpi.LocalEndPoint.Port == port)
                {
                    return false;
                }
            }
            return true;
        }

        #endregion

        #region Protected Methods and Events

        public event EventHandler StateChanged;
        protected virtual void OnChangedState(EventArgs args)
        {
            var ev = StateChanged;
            if (ev != null)
            {
                ev(this, args);
            }
        }

        #endregion

        #region Properties

        public HTTPServerState.STATE State
        {
            get { return _state; }
            private set
            {
                _state = value;
                OnChangedState(EventArgs.Empty);
            }
        }

        public int Port { get; private set; }

        public IPEndPoint EndPoint { get; set; }

        public int ReadBufferSize { get; set; }

        public int WriteBufferSize { get; set; }

        public string ServerBanner { get; set; }

        public TimeSpan ReadTimeout { get; set; }

        public TimeSpan WriteTimeout { get; set; }

        public TimeSpan ShutdownTimeout { get; set; }

        internal HttpServerUtility ServerUtility { get; private set; }

        internal HttpTimeoutManager TimeoutManager { get; private set; }


        #endregion


    }
}
