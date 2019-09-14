using System;
using core.Shared;

namespace core.Portfolio
{
    public class OptionObtained : AggregateEvent
    {

        public OptionObtained(string ticker, OptionType type, double strikePrice, DateTimeOffset expiration, string userId, DateTime when)
            : base(OwnedOption.GenerateKey(ticker, type, expiration, strikePrice), userId, when)
        {
            this.TickerSymbol = ticker;
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