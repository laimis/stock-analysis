using System;
using core.Shared;

namespace core.Options
{
    public class OptionSold : AggregateEvent
    {
        public OptionSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string ticker,
            OptionType type,
            double strikePrice,
            DateTimeOffset expiration,
            string userId,
            int amount,
            double premium)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.Type = type;
            this.StrikePrice = strikePrice;
            this.Expiration = expiration;
            this.UserId = userId;
            this.Amount = amount;
            this.Premium = premium;
        }

        public string Ticker { get; }
        public OptionType Type { get; }
        public double StrikePrice { get; }
        public DateTimeOffset Expiration { get; }
        public string UserId { get; }
        public int Amount { get; }
        public double Premium { get; }
    }
}