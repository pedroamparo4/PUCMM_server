using System;

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
