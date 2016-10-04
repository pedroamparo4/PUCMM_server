using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server
{
    class Program
    {
        static void Main(string[] args)
        {
            int? port = null;
            string path = string.Empty;
            int default_port = 80;
            string default_path = Directory.GetCurrentDirectory();
            int port_index;
            int path_index;

            port_index = args.ToList().IndexOf("--port");
            path_index = args.ToList().IndexOf("--path");

            if (port_index > -1 && (port_index+1) < (args.Length))
            {
                try
                {
                    port = Convert.ToInt16(args[port_index+1]);
                }
                catch
                {
                    Console.WriteLine("The given value for PARAM [port] is not an integer");
                    return;
                }
               
            }
            if (path_index > -1 && (path_index + 1) < (args.Length))
            {
                path = args[path_index + 1];
                if (string.IsNullOrEmpty(path))
                {
                    Console.WriteLine("The given value for PARAM [path] cannot be empty");
                    return;
                }
            }

            if (port == null)
            {
                port = default_port;
            }

            if (string.IsNullOrEmpty(path))
            {
                path = default_path;
            }

            Console.WriteLine("PORT: " + port);
            Console.WriteLine("PATH: " + path);
        }


    }
}

