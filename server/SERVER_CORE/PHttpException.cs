using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace server.SERVER_CORE
{
    [Serializable]
    public class PHttpException : Exception
    {
        public PHttpException()
        {
        }

        public PHttpException(string message)
            : base(message)
        {
        }

        public PHttpException(string message, Exception innerException)
            : base(message, innerException)
        {
        }

        protected PHttpException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }
}