using System;
using System.Collections.Generic;
using System.Text;

namespace server.SERVER_CORE
{
    public class HttpContext
    {
        internal HttpContext(HTTPClient client)
        {
            Server = client.Server.ServerUtility;
            Request = new HttpRequest(client);
            Response = new HttpResponse(this);
        }

        public HttpServerUtility Server { get; private set; }

        public HttpRequest Request { get; private set; }

        public HttpResponse Response { get; private set; }
    }
}