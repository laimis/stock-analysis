using System;
using core.Shared;

namespace core.Stocks
{
    internal class TickerObtained : AggregateEvent
    {
        public TickerObtained(string ticker, string userId, DateTime when) : base(ticker, userId, when)
        {
        }
    }
}