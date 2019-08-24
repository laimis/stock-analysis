using System;

namespace core.Portfolio
{
	internal class TickerObtained : AggregateEvent
	{
		public TickerObtained(string ticker, string userId, DateTime when) : base(ticker, userId, when)
		{
		}
	}
}