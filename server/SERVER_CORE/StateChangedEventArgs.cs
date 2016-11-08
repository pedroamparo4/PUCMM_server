using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace server.SERVER_CORE
{
    public class StateChangedEventArgs : EventArgs
    {
        public HTTPServerState.STATE CurrentState { get; private set; }

        public HTTPServerState.STATE PreviousState { get; private set; }

        public StateChangedEventArgs(HTTPServerState.STATE previousState, HTTPServerState.STATE currentState)
        {
            PreviousState = previousState;
            CurrentState = currentState;
        }
    }
}
