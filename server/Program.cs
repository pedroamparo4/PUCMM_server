using System;
using System.IO;
using System.Linq;


namespace server
{
    class Program
    {
        static int Main(string[] args)
        {
            int? port = null;
            int aux;
            string path = string.Empty;
            string default_path = Directory.GetCurrentDirectory();
            int port_index;
            int path_index;
            SERVER_CORE.Enviroment enviroment = new SERVER_CORE.Enviroment();
            string _input; 
            

            port_index = args.ToList().IndexOf("--port");
            path_index = args.ToList().IndexOf("--path");

            if (port_index > -1 && (port_index+1) < (args.Length))
            {
                if(!int.TryParse(args[port_index + 1], out aux))
                {
                    Console.WriteLine("The given value for PARAM [port] is not an integer");
                    return 1;
                }

                port = aux;            
            }
            if (path_index > -1 && (path_index + 1) < (args.Length))
            {
                path = args[path_index + 1];
                if (string.IsNullOrEmpty(path))
                {
                    Console.WriteLine("The given value for PARAM [path] cannot be empty");
                    return 1;
                }

                if (!Directory.Exists(path))
                {
                    Console.WriteLine("The given path does not exist");
                    return 1;
                }
            }

            if (port == null)
            {
                port = SERVER_CORE.CORE.default_port;
            }

            if (string.IsNullOrEmpty(path))
            {
                path = default_path;
            }

            if(!SERVER_CORE.CORE.PortIsAvailable(port))
            {
                Console.WriteLine($"PORT {port} is busy");
                return 1;
            }

            Console.WriteLine("PORT: " + port);
            Console.WriteLine("PATH: " + path);

            

            while(true)
            {
                _input = Console.ReadLine();
                _input = _input.ToLower();

                if(_input.Equals(SERVER_CORE.CORE._timestamp_command))
                {
                    Console.WriteLine("UPTIME: "+ (DateTime.Now - enviroment.App_Start_Timestamp).TotalMinutes+" minutes");
                }
            }
  
            return 0;
        }

      
    }
}

