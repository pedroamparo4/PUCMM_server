using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace server.SERVER_CORE
{
    public static class CORE
    {
        public const string _timestamp_command = "uptime";
        const int default_port = 80;

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

    }
}
