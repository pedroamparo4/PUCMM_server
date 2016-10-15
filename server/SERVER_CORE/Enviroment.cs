using System;

namespace server.SERVER_CORE
{
    public class Enviroment
    {
        private DateTime _app_start_timestamp;
        private string _default_path;

        public Enviroment(string default_path)
        {
            this._app_start_timestamp = DateTime.Now;
            this._default_path = default_path;
        }

        public DateTime App_Start_Timestamp
        {
            get { return this._app_start_timestamp; }
        }

        public string DefaultPath
        {
            get { return this._default_path; }
        }

    }
}
