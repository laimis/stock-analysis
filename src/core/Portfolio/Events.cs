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

    internal class StockListTagAdded : AggregateEvent
    {
        public StockListTagAdded(Guid id, Guid aggregateId, DateTimeOffset when, string tag)
            : base(id, aggregateId, when)
        {
            Tag = tag;
        }

        public string Tag { get; }
    }

    internal class StockListTagRemoved : AggregateEvent
    {
        public StockListTagRemoved(Guid id, Guid aggregateId, DateTimeOffset when, string tag)
            : base(id, aggregateId, when)
        {
            Tag = tag;
        }

        public string Tag { get; }
    }

    internal class PendingStockPositionCreated : AggregateEvent
    {
        public PendingStockPositionCreated(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string ticker,
            decimal price,
            decimal numberOfShares,
            decimal? stopPrice,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            Price = price;
            NumberOfShares = numberOfShares;
            StopPrice = stopPrice;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal Price { get; }
        public decimal NumberOfShares { get; }
        public decimal? StopPrice { get; }
        public string Notes { get; }
    }

    public class PendingStockPositionDeleted : AggregateEvent
    {
        public PendingStockPositionDeleted(Guid id, Guid aggregateId, DateTimeOffset when)
            : base(id, aggregateId, when)
        {
        }
    }
}