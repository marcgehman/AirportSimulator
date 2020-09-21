using System;
using System.Timers;
using System.Threading;       

namespace AirportSimulator
{

    public class RequestResult {
        public enum RequestState {
            Hold,
            Proceed
        }

        public RequestState state {get; private set;}

        public RequestToken token {get; private set;} 

        public RequestResult(RequestState state, RequestToken token)
        {
            this.state = state;
            this.token = token;
        }

    }

}