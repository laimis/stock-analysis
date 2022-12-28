using System;
using core.Shared;

namespace core.Portfolio
{
    internal class StockListCreated : AggregateEvent
    {
        public StockListCreated(Guid id, Guid aggregateId, DateTimeOffset when, string description, string name, Guid userId)
            : base(id, aggregateId, when)
        {
            Description = description;
            Name = name;
            UserId = userId;
        }

        public string Description { get; }
        public string Name { get; }
        public Guid UserId { get; }
    }

    internal class StockListUpdated : AggregateEvent
    {
        public StockListUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string description, string name)
            : base(id, aggregateId, when)
        {
            Description = description;
            Name = name;
        }

        public string Description { get; }
        public string Name { get; }
    }

    internal class StockListTickerAdded : AggregateEvent
    {
        public StockListTickerAdded(Guid id, Guid aggregateId, DateTimeOffset when, string note, string ticker)
            : base(id, aggregateId, when)
        {
            Note = note;
            Ticker = ticker;
        }

        public string Note { get; }
        public string Ticker { get; }
    }

    internal class StockListTickerRemoved : AggregateEvent
    {
        public StockListTickerRemoved(Guid id, Guid aggregateId, DateTimeOffset when, string ticker)
            : base(id, aggregateId, when)
        {
            Ticker = ticker;
        }

        public string Ticker { get; }
    }
}