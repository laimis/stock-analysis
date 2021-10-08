using System;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class OptionDeleted : AggregateEvent
    {
        public OptionDeleted(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class OptionExpired : AggregateEvent
    {
        public OptionExpired(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            bool assigned)
            : base(id, aggregateId, when)
        {
            this.Assigned = assigned;
        }

        public bool Assigned { get; }
    }

    public class OptionOpened : AggregateEvent
    {
        public OptionOpened(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string ticker,
            decimal strikePrice,
            OptionType optionType,
            DateTimeOffset expiration,
            Guid userId)
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
        public decimal StrikePrice { get; }
        public DateTimeOffset Expiration { get; }
        public Guid UserId { get; }
    }

    public class OptionPurchased : AggregateEvent, INotification
    {
        public OptionPurchased(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            int numberOfContracts,
            decimal premium,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.NumberOfContracts = numberOfContracts;
            this.Premium = premium;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public int NumberOfContracts { get; }
        public decimal Premium { get; }
        public string Notes { get; }
    }

    public class OptionSold : AggregateEvent, INotification
    {
        public OptionSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            int numberOfContracts,
            decimal premium,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.NumberOfContracts = numberOfContracts;
            this.Premium = premium;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public int NumberOfContracts { get; }
        public decimal Premium { get; }
        public string Notes { get; }
    }
}