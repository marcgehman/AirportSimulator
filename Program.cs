using System;
using System.Threading;
using System.Collections.Generic;

namespace AirportSimulator
{
    class Program
    {
        private const uint NumberOfClients = 30;
        private const uint NumberOfRunways = 5;
        private const uint NumberOfParkingStands = 10;
        
        private const int RandomDelayMax = 5;
        
        static void Main(string[] args)
        {
            var airport = new Airport(NumberOfRunways, NumberOfParkingStands); 
            // Now spin a number of threads simulating some aircrafts.

            var aircraftClients = new List<Thread>();
            var randomizer = new Random();

            for (int i = 0; i < NumberOfClients; i++)
            {
                var worker = new Thread(() => {

                    Guid aircraftId = Guid.NewGuid();
                    bool landingSuccessful = false;

                    while (!landingSuccessful)
                    {
                        Console.WriteLine("Worker " + aircraftId.ToString() + " is attempting a landing.");                        

                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // Attempt calling into Airport.RequestLanding until landing token is successfully received.
                        // Repeat otherwise.
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        RequestResult landingRequestResult = airport.RequestLanding(aircraftId);
                        while(landingRequestResult.state != RequestResult.RequestState.Proceed)
                        {
                            Thread.Sleep(200 * randomizer.Next(RandomDelayMax));
                            landingRequestResult = airport.RequestLanding(aircraftId);
                          
                        }
                        Console.WriteLine("Worker " + aircraftId.ToString() + " received a landing token.");

                        // Wait some random time. In some cases the token will expire, that's OK.
                        Thread.Sleep(1000 * randomizer.Next((int)Airport.TokenValiditySeconds*2));

                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // Attempt calling into Airport.PerformLanding and break out of the loop if it's successful.
                        // Allow the loop to repeat otherwise.
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        PerformResult landingPerformResult = airport.PerformLanding((LandingRequestToken)landingRequestResult.token);
                        if(landingPerformResult == PerformResult.Success)
                        {           
                            landingSuccessful = true;
                        }
                        if (!landingSuccessful)
                        {
                            Console.WriteLine("Worker " + aircraftId.ToString() + " landing token expired. It will try again.");
                        }
                    }

                    Console.WriteLine("Worker " + aircraftId.ToString() + " has successfully landed.");

                    // Sleep for at least the time of landing operation plus some random time
                    Thread.Sleep((int)(1000 * (Airport.OperationDurationSeconds + randomizer.Next(RandomDelayMax))));

                    Console.WriteLine("Worker " + aircraftId.ToString() + " is attempting a take-off.");

                    bool takeOffSuccessful = false;
                
                    while (!takeOffSuccessful)
                    {
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // Attempt calling into Airport.RequestTakeoff until take-off token is successfully received.
                        // Repeat otherwise.
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        
                        RequestResult takeOffRequestResult = airport.RequestTakeOff(aircraftId);
                        while(takeOffRequestResult.state != RequestResult.RequestState.Proceed)
                        {
                            Thread.Sleep(200 * randomizer.Next(RandomDelayMax));
                            takeOffRequestResult = airport.RequestTakeOff(aircraftId);
                        }
                       Console.WriteLine("Worker " + aircraftId.ToString() + " received a take-off token.");

                       // Wait some random time. In some cases the token will expire, that's OK.
                       Thread.Sleep(1000 * randomizer.Next((int)Airport.TokenValiditySeconds*2));

                        /////////////////////////////////////////////////////////////////////////////////////////////////////////
                        // Attempt calling into Airport.PerformTakeoff and break out of the loop if it's successful.
                        // Allow the loop to repeat otherwise.
                        /////////////////////////////////////////////////////////////////////////////////////////////////////////

                        PerformResult takeOffPerformResult = airport.PerformTakeOff((TakeOffRequestToken)takeOffRequestResult.token);
                        if(takeOffPerformResult == PerformResult.Success)
                        {
                            takeOffSuccessful = true;
                        }

                        if (!takeOffSuccessful)
                        {
                            Console.WriteLine("Worker " + aircraftId.ToString() + " take-off token expired. It will try again.");
                        }
                    }
                    
                   Console.WriteLine("Worker " + aircraftId.ToString() + " has successfully departed.");

                });

                worker.Start();
                aircraftClients.Add(worker);
            }

            foreach (var client in aircraftClients) {
                client.Join();
            }

            Console.WriteLine("All done. Simulation terminating.");
        }
    }
}
