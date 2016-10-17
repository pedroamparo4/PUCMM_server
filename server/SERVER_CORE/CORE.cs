using System.IO;
using System.Linq;
using System.Net.NetworkInformation;

namespace server.SERVER_CORE
{
    public static class CORE
    {
        public const string _timestamp_minutes_running_command = "uptime";
        public const string _exit_app_command = "exit";
        public const string _timestamp_started_at_command = "uptime-attime";
        public const int _app_response_fail = 1;
        public const int _app_response_success = 0;
        public const int _default_port = 80;
        public const string _server_banner_name = "PUCMM_HTTP";
        public const int _ReadBufferSize = 4096;
        public const int _WriteBufferSize = 4096;

        public static bool PortIsAvailable(int? port)
        {
            IPGlobalProperties ipGlobalProperties = IPGlobalProperties.GetIPGlobalProperties();
            TcpConnectionInformation[] tcpConnInfoArray = ipGlobalProperties.GetActiveTcpConnections();

            if (port == null)
            {
                return false;
            }

            var existing_port = tcpConnInfoArray.Where(p => p.LocalEndPoint.Port == port).FirstOrDefault();
            if (existing_port != null)
            {
                return false;
            }

            return true;
        }

        public static InitialParams ParseParams(string[] args)
        {
            int port_index;
            int path_index;
            int aux;
            InitialParams initial_params = new InitialParams { _path = null, _port = null };

            port_index = args.ToList().IndexOf("--port");
            path_index = args.ToList().IndexOf("--path");

            if (port_index > -1 && (port_index + 1) < (args.Length))
            {
                if (!int.TryParse(args[port_index + 1], out aux))
                {
                    throw new System.Exception("The given value for PARAM [port] is not an integer");
                }

                initial_params._port = aux;
            }
            if (path_index > -1 && (path_index + 1) < (args.Length))
            {
                initial_params._path = args[path_index + 1];
                if (string.IsNullOrEmpty(initial_params._path))
                {
                    throw new System.Exception("The given value for PARAM [path] cannot be empty");
                }

                if (!Directory.Exists(initial_params._path))
                {
                    throw new System.Exception("The given path does not exist");
                }
            }

            return initial_params;
        }

    }
}
