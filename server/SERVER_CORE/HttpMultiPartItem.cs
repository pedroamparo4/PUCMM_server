using System;
using System.Collections.Generic;
using System.IO;

namespace server.SERVER_CORE
{
    internal class HttpMultiPartItem
    {
        public HttpMultiPartItem(Dictionary<string, string> headers, string value, Stream stream)
        {
            if (headers == null)
                throw new ArgumentNullException("headers");

            Headers = headers;
            Value = value;
            Stream = stream;
        }

        public Dictionary<string, string> Headers { get; private set; }

        public string Value { get; private set; }

        public Stream Stream { get; private set; }
    }
}