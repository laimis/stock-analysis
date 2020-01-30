using System;
using core.Shared;

namespace core.Options
{
    public class OptionOpened : AggregateEvent
    {
        public OptionOpened(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string ticker,
            double strikePrice,
            OptionType optionType,
            DateTimeOffset expiration,
            string userId)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.OptionType = optionType;
            this.StrikePrice = strikePrice;
            this.Expiration = expiration;
            this.UserId = userId;
        }

        public string Ticker { get; }
        public OptionType OptionType { get; }
        public double StrikePrice { get; }
        public DateTimeOffset Expiration { get; }
        public string UserId { get; }
    }
}