using System;

namespace AirportSimulator
{
	public class LandingRequestToken : RequestToken
	{
		public Guid aircraftId {get;}
		public Guid runwayId {get;}
		public Guid parkingStandId {get;}
		public LandingRequestToken(uint tokenValiditySeconds, Guid aircraftId, Guid runwayId, Guid parkingStandId) : base(tokenValiditySeconds)
		{
			this.aircraftId = aircraftId;
			this.runwayId = runwayId;
			this.parkingStandId = parkingStandId;
		}
	}
}
