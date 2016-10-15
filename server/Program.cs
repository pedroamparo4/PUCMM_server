using System;
using System.IO;
using System.Linq;


namespace server
{
    class Program
    {
        static int Main(string[] args)
        {
            bool server_is_running = true;
            string path = null;
            int? port = null;
            string _input;
            SERVER_CORE.Enviroment enviroment = new SERVER_CORE.Enviroment(Directory.GetCurrentDirectory());
            SERVER_CORE.InitialParams parsed_params;

            try
            {
                parsed_params = SERVER_CORE.CORE.ParseParams(args);
                port = parsed_params._port;
                path = parsed_params._path;
            }
            catch(Exception e)
            {
                Console.WriteLine(e.Message);
                return SERVER_CORE.CORE._app_response_fail;
            }       

            if (parsed_params._port == null)
            {
                port = SERVER_CORE.CORE.default_port;
            }

            if (string.IsNullOrEmpty(parsed_params._path))
            {
                path = enviroment.DefaultPath;
            }

            if(!SERVER_CORE.CORE.PortIsAvailable(port))
            {
                Console.WriteLine($"PORT {port} is busy");
                return SERVER_CORE.CORE._app_response_fail;
            }

            Console.WriteLine("PORT: " + port);
            Console.WriteLine("PATH: " + path);          

            //Infinite loop... until 'exit' command is typed
            while(server_is_running)
            {
                _input = Console.ReadLine();
                _input = _input.ToLower();

                switch(_input)
                {
                    case SERVER_CORE.CORE._timestamp_minutes_running_command:
                        Console.WriteLine("UPTIME: " + (DateTime.Now - enviroment.App_Start_Timestamp).TotalMinutes + " minutes");
                        break;

                    case SERVER_CORE.CORE._timestamp_started_at_command:
                        Console.WriteLine($"UPTIME STARTED AT: {enviroment.App_Start_Timestamp.ToShortDateString()} | {enviroment.App_Start_Timestamp.ToShortTimeString()}");
                        break;

                    case SERVER_CORE.CORE._exit_app_command:
                        server_is_running = false;
                        break;

                    default:
                        Console.WriteLine($"UNKNOWN COMMAND '{_input}'");
                        break;
                }
            }

            return SERVER_CORE.CORE._app_response_success;
        }

      
    }
}

