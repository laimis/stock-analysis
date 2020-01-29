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
            PositionType positionType,
            OptionType optionType,
            double strikePrice,
            DateTimeOffset expiration,
            string userId,
            int numberOfContracts,
            double premium)
            : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.PositionType = positionType;
            this.OptionType = optionType;
            this.StrikePrice = strikePrice;
            this.Expiration = expiration;
            this.UserId = userId;
            this.NumberOfContracts = numberOfContracts;
            this.Premium = premium;
        }

        public string Ticker { get; }
        public PositionType PositionType { get; }
        public OptionType OptionType { get; }
        public double StrikePrice { get; }
        public DateTimeOffset Expiration { get; }
        public string UserId { get; }
        public int NumberOfContracts { get; }
        public double Premium { get; }
    }
}