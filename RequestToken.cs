using System;
using System.Timers;
using System.Threading;

namespace AirportSimulator
{
		public class RequestToken : System.Timers.Timer
		{
			public DateTime expiration {get;}
			public bool hasExpired {get; private set;}
            public RequestToken(uint tokenValiditySeconds)
			{
				// Setup timer parameters
				Interval = tokenValiditySeconds * 1000;
				AutoReset = false;
				Elapsed += OnTimerElapsed;
				
				hasExpired = false;
				// Sets the time when the token will expire.
				expiration = new DateTime();
				TimeSpan seconds = TimeSpan.FromSeconds(tokenValiditySeconds);
				expiration = DateTime.Now + seconds;

				// Start the timer as soon as the token is created. 
				// Timer will expire shortly after the initialized "expiration" time.
				// Timer is used by the airport to release its resources asynchronously.
				// This allows reserved resources to be released if request token is never used.
				Start();
			}
			
			private void OnTimerElapsed(object source,ElapsedEventArgs e)
			{
				hasExpired = true;
			}

		}
}