using System;

namespace core.Shared
{
	public class AggregateEvent
	{
		public AggregateEvent(string ticker, string userId, DateTime when)
		{
			this.Ticker = ticker;
			this.UserId = userId;
			this.When = when;
		}

		public DateTime When { get; }
		public string Ticker { get; }
		public string UserId { get; }
	}
}