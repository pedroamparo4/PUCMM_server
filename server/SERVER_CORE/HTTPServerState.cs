using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.SERVER_CORE
{
    public class HTTPServerState
    {
        public enum STATE
        {
            STARTING = 1,
            STARTED = 2,
            STOPPING = 3,
            STOPPED = 4
        };
    }
}
