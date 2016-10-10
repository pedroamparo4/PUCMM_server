using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.SERVER_CORE
{
    public class Enviroment
    {
        private DateTime _app_start_timestamp;

        public Enviroment()
        {
            this._app_start_timestamp = DateTime.Now;
        }

        public DateTime App_Start_Timestamp
        {
            get { return this._app_start_timestamp; }
        }

    }
}
