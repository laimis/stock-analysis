using System;

namespace core.Portfolio
{
	internal class AggregateEvent
	{
		public AggregateEvent(string userId, DateTime when)
		{
			this.UserId = userId;
			this.When = when;
		}

		public DateTime When { get; }
		public string UserId { get; }
	}
}