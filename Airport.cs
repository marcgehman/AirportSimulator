using System;
using System.Timers;
using System.Collections.Generic;
using System.Threading;

namespace AirportSimulator
{

    public class Airport
    {
        
        public class Runway : System.Timers.Timer
        {
            public enum RunwayState
            {
                InOperation,
                Reserved,
                Available
            }
            public RunwayState state {get; set;} 
            public Guid id {get;}
            public uint length {get;}
            public Guid aircraftId{get;set;}

            public Runway(uint length)
            {
                state = RunwayState.Available;
                id = Guid.NewGuid();
                this.length = length;

                // Create a timer for the operation duration and set its interval to the length in seconds.
                Interval = length * 1000;
                AutoReset = false;
            }

            // Operates runway for landing or takeoff.
            public void Operate()
            {
                state = RunwayState.InOperation;
                // Start the runway's timer.
                Start();
            }
            
        }


        public class ParkingStand
        {
            public enum ParkingStandState
            {
                Occupied,
                Reserved,
                Available
            }
            public ParkingStandState state {get;set;}
            public Guid id {get;}
            public Guid aircraftId {get; set;}

            public ParkingStand()
            {
                state = ParkingStandState.Available;
                id = Guid.NewGuid();
                aircraftId = Guid.Empty;
            }
        }


        public static readonly uint TokenValiditySeconds = 2;
        public static readonly uint OperationDurationSeconds = 5;
        private List<Runway> runways {get;set;}
        private List<ParkingStand> parkingStands {get;set;}
        private Mutex mutex;

        public Airport(uint numberOfRunways, uint numberOfParkingStands)
        {

            runways = new List<Runway>();
            for(int i = 0; i < numberOfRunways; i++)
            {
                runways.Add(new Runway(OperationDurationSeconds));
            }

            parkingStands = new List<ParkingStand>();
            for(int i = 0; i < numberOfParkingStands; i++)
            {
                parkingStands.Add(new ParkingStand());
            }

            mutex = new Mutex();

        }


        // Returns a runway matching the runwayId if found.
        // Otherwise, returns null.
        private Runway GetRunwayById(Guid runwayId)
        {
              for(int i = 0; i < runways.Count; i++)
            {
                if(runways[i].id == runwayId)
                {
                    return runways[i];
                }
            }
            return null;
        }

        // Returns a parking stand matching the parkingStandId if found.
        // Otherwise, returns null.
        private ParkingStand GetParkingStandById(Guid parkingStandId)
        {
            for(int i = 0; i < parkingStands.Count; i++)
            {
                if(parkingStands[i].id == parkingStandId)
                {
                    return parkingStands[i];
                }
            }
            return null;
        }

        // Returns a parking stand matching the aircraftId if found.
        // Otherwise, returns null.
        private ParkingStand GetParkingStandByAircraftId(Guid aircraftId)
        {
            for(int i = 0; i < parkingStands.Count; i++)
            {
                if(parkingStands[i].aircraftId == aircraftId)
                {
                    return parkingStands[i];
                }
            }
            return null;
        }

        // Reserves and returns an available runway if found.
        // Otherwise, returns an empty Guid.
        private Guid ReserveAvailableRunway()
        {
            for(int i = 0; i < runways.Count; i++)
            {
                if(runways[i].state == Runway.RunwayState.Available)
                {
                    runways[i].state = Runway.RunwayState.Reserved;
                    return runways[i].id;
                }
            }
            return Guid.Empty;
        }

        // Release resources reserved by a landing request token that will not be used.
        private void ReleaseLandingResources(LandingRequestToken token)
        {
            Runway runway = GetRunwayById(token.runwayId);
            ParkingStand parkingStand = GetParkingStandById(token.parkingStandId);
    
            runway.state = Runway.RunwayState.Available;
            runway.aircraftId = Guid.Empty;
            parkingStand.aircraftId = Guid.Empty;
            parkingStand.state = ParkingStand.ParkingStandState.Available;
            token.Dispose();
        }
        
        // Release resources resereved by a takeoff request token that will not be used.
        private void ReleaseTakeOffResources(TakeOffRequestToken token)
        {
            Runway runway = GetRunwayById(token.runwayId);
            runway.state = Runway.RunwayState.Available;
            runway.aircraftId = Guid.Empty;
            token.Dispose();
        }
        // Reserves and returns an available parking stand if found.
        // Otherwise, returns an empty Guid.
        private Guid ReserveAvailableParkingStand()
        {
            for(int i = 0; i < parkingStands.Count; i++)
            {
                if(parkingStands[i].state == ParkingStand.ParkingStandState.Available)
                {
                    parkingStands[i].state = ParkingStand.ParkingStandState.Reserved;
                    return parkingStands[i].id;
                }
            }
            return Guid.Empty;
        }

        // Returns the number of available runways.
        private int NumberOfAvailableRunways()
        {
            int count = 0;
            for(int i = 0; i < runways.Count; i++)
            {
                if(runways[i].state == Runway.RunwayState.Available)
                {
                    count++;
                }
            }
            return count;

        }

        // Returns the number of available parking stands.
        private int NumberOfAvailableParkingStands()
        {
            int count = 0;
            for(int i = 0; i < parkingStands.Count; i++)
            {
                if(parkingStands[i].state == ParkingStand.ParkingStandState.Available)
                {
                    count++;
                }
            }
            return count;
        }

        /* After a successful call, the resources necessary to perform the operation
           should be reserved for the time of the returned authorization. */
        public RequestResult RequestLanding(Guid aircraftId)
        {
            mutex.WaitOne();

            // A runway and a parking stand are available.
            if(NumberOfAvailableRunways() > 0 && NumberOfAvailableParkingStands() > 0)
            {
                // Reserve and return the resources' ids
                Guid runwayId = ReserveAvailableRunway();
                Guid parkingStandId = ReserveAvailableParkingStand();
                
                // Create the landing request token
                LandingRequestToken token = new LandingRequestToken(TokenValiditySeconds,aircraftId,runwayId,parkingStandId);
                
                // Subscribe for token's expiration to ensure reserved resources can be released.
                token.Elapsed += OnExpiredLandingToken;

                mutex.ReleaseMutex();
                return new RequestResult(RequestResult.RequestState.Proceed, token);
            }
            else
            {
                mutex.ReleaseMutex();
                return new RequestResult(RequestResult.RequestState.Hold, null);
            }
           
           
        }

       /* After a successful call, the resources necessary to perform the operation
          should be reserved for the time of the returned authorization. */
        public RequestResult RequestTakeOff(Guid aircraftId)
        {
            mutex.WaitOne();
            // A runway is available.
            if(NumberOfAvailableRunways() > 0)
            {
                Guid runwayId = ReserveAvailableRunway();

                // Create the takeoff request token
                TakeOffRequestToken token = new TakeOffRequestToken(TokenValiditySeconds, aircraftId, runwayId);

                // Subscribe for token's expiration to ensure reserved resources can be released.
                token.Elapsed += OnExpiredTakeOffToken;

                mutex.ReleaseMutex();
                return new RequestResult(RequestResult.RequestState.Proceed, token);
            }
            else
            {
                mutex.ReleaseMutex();
                return new RequestResult(RequestResult.RequestState.Hold, null);
            }
        }

        /* After this call is executed successfully, the used runway is InOperation
           for the length of operation duration parameter and can't handle any other
           operations during this time. After the operation is completed, the runway
           becomes Available again, and the reserved parking stand becomes
           Occupied. The call is non-blocking, i.e. it returns before the runway is
           cleared. */
        public PerformResult PerformLanding(LandingRequestToken token)
        {
            token.Stop();
            token.Elapsed -= OnExpiredLandingToken;
            // Token hasn't expired yet
            if(DateTime.Now <= token.expiration && !token.hasExpired)
            {
                Runway runway = GetRunwayById(token.runwayId);
                // Check if the runway exists and if it has been properly reserved
                if(runway == null || runway.state != Runway.RunwayState.Reserved)
                {
                    ReleaseLandingResources(token);
                    return PerformResult.InvalidParameters;
                }
                
                ParkingStand parkingStand = GetParkingStandById(token.parkingStandId);
                // Check if the parkingStand exists and if it has been properly reserved
                if (parkingStand == null|| parkingStand.state != ParkingStand.ParkingStandState.Reserved)
                {
                    ReleaseLandingResources(token);
                    return PerformResult.InvalidParameters;
                }
                runway.aircraftId = token.aircraftId;
                parkingStand.aircraftId = runway.aircraftId;
                runway.Elapsed += OnLandingOperationComplete;
                
                token.Dispose();
                runway.Operate();
                return PerformResult.Success;
            }

            
            else // Token has expired
            {
                
                if (!token.hasExpired) // the token's expiration date has passed but the elapsed event hasn't fired yet, then release the resources.
                {
                  ReleaseLandingResources(token);
                }
                return PerformResult.ExpiredToken;
            }
        }

        /* After this call is executed successfully, the occupied parking stand
           becomes Available,and the used runway is InOperation for the
           length of operation duration parameter and can't handle any other
           operations during this time. After operation finishes, the runway becomes
           Available again. The call is non-blocking, i.e. it returns before the
           runway is cleared. */
        public PerformResult PerformTakeOff(TakeOffRequestToken token)
        {
            token.Stop();
            token.Elapsed -= OnExpiredTakeOffToken;
            if(DateTime.Now <= token.expiration && !token.hasExpired)
            {
                Runway runway = GetRunwayById(token.runwayId);
                // Check if the runway exists and if it has been properly reserved
                if(runway == null|| runway.state != Runway.RunwayState.Reserved)
                {
                    ReleaseTakeOffResources(token);
                    return PerformResult.InvalidParameters;
                }
                runway.aircraftId = token.aircraftId;
                runway.Elapsed += OnTakeOffOperationComplete;

                ParkingStand parkingStand = GetParkingStandByAircraftId(token.aircraftId);  
                parkingStand.aircraftId = Guid.Empty;
                parkingStand.state = ParkingStand.ParkingStandState.Available;

                token.Dispose();
                runway.Operate();
                return PerformResult.Success;
            }

            else // Token has expired
            {
                if (!token.hasExpired) // If the token's expiration date has passed but the elapsed event hasn't fired yet, then release the resources
                {
                    ReleaseTakeOffResources(token); 
                }
                return PerformResult.ExpiredToken;
            }
        
        }

        // Releases resources when the landing token's timer elapses.
        private void OnExpiredLandingToken(object source, ElapsedEventArgs e)
        {
            LandingRequestToken token = (LandingRequestToken)source;
            // Unsubscribe from the token's timer.
            token.Elapsed -= OnExpiredLandingToken;

            // Release the token's resources.
            ReleaseLandingResources(token);
        }


         // Releases resources when the takeoff token's timer elapses.
        private void OnExpiredTakeOffToken(object source, ElapsedEventArgs e)
        {  
            TakeOffRequestToken token = (TakeOffRequestToken)source;
            // Unsubscribe from the token's timer.
            token.Elapsed -= OnExpiredTakeOffToken;

            // Release the token's resources.
            ReleaseTakeOffResources(token);
        }

        // Releases resources and updates states accordingly when the landing operation completes (runway's timer elapses).
        private void OnLandingOperationComplete(object source, ElapsedEventArgs e)
        {
            Runway runway = (Runway)source;            
            // Unsubscribe from the runway's timer
            runway.Elapsed -= OnLandingOperationComplete;

            ParkingStand parkingStand = GetParkingStandByAircraftId(runway.aircraftId);
            
            // Update the states and data.
            runway.state = Runway.RunwayState.Available;
            parkingStand.state = ParkingStand.ParkingStandState.Occupied;
            runway.aircraftId = Guid.Empty;
            
            
        }

        // Releases resources and updates states accordingly when the takeoff operation completes (runway timer elapses).
        private void OnTakeOffOperationComplete(object source, ElapsedEventArgs e)
        {
            Runway runway = (Runway)source;
            // Unsubscribe from the runway's timer.
            runway.Elapsed -= OnTakeOffOperationComplete;    

            // Update state and data.
            runway.state = Runway.RunwayState.Available;
            runway.aircraftId = Guid.Empty;
           
        }
        
    }
}