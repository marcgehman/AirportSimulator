using System;

namespace AirportSimulator
{

	public class TakeOffRequestToken : RequestToken
	{
		public Guid aircraftId {get;}
		public Guid runwayId {get;}
		public TakeOffRequestToken(uint tokenValiditySeconds, Guid aircraftId, Guid runwayId) : base(tokenValiditySeconds)
		{
			this.aircraftId = aircraftId;
			this.runwayId = runwayId;
		}
	}
}