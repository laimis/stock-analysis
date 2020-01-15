using System;
using core.Shared;

namespace core.Portfolio
{
    public class OptionObtained : AggregateEvent
    {

        public OptionObtained(string tickerSymbol, OptionType type, double strikePrice, DateTimeOffset expiration, string userId, DateTime when)
            : base(SoldOption.GenerateKey(tickerSymbol, type, expiration, strikePrice), userId, when)
        {
            this.TickerSymbol = tickerSymbol;
            this.Type = type;
            this.StrikePrice = strikePrice;
            this.Expiration = expiration;
        }

        public string TickerSymbol { get; }
        public OptionType Type { get; }
        public double StrikePrice { get; }
        public DateTimeOffset Expiration { get; }
    }
}