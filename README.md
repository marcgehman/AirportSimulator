# AirportSimulator
A small Airport Simulator that I developed to study parallel and asynchronous programming.

Airplanes contend for limited resources (runways, parking stands). Each airplane must make requests to the airport to takeoff/land. If a resource is available,
the airplane receives a takeoff or landing token which is valid for a finite amount of time.
If the token expires, they cannot execute the landing/takeoff and must request another token.

The goal for every airplane in this simulation is to land, park at a parking stand, and then depart.
