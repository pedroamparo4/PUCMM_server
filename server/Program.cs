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

            if (args.GetLength(0) > 0)
            {
                port = Convert.ToInt16(args[0]);
            }
            if (args.GetLength(0) > 1)
            {
                path = args[1];
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
            Console.ReadLine();
        }
    }
}
