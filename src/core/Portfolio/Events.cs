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
    
    internal class PendingStockPositionCreatedWithStrategy : AggregateEvent
    {
        public PendingStockPositionCreatedWithStrategy(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string notes,
            decimal numberOfShares,
            decimal price,
            decimal? stopPrice,
            string strategy,
            string ticker,
            Guid userId)
            : base(id, aggregateId, when)
        {
            Notes = notes;
            NumberOfShares = numberOfShares;
            Price = price;
            StopPrice = stopPrice;
            Strategy = strategy;
            Ticker = ticker;
            UserId = userId;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal Price { get; }
        public decimal NumberOfShares { get; }
        public decimal? StopPrice { get; }
        public string Notes { get; }
        public string Strategy { get; }
    }

    public class PendingStockPositionClosed : AggregateEvent
    {
        public PendingStockPositionClosed(Guid id, Guid aggregateId, DateTimeOffset when, bool purchased, decimal? price)
            : base(id, aggregateId, when)
        {
            Purchased = purchased;
            Price = price;
        }

        public bool Purchased { get; }
        public decimal? Price { get; }
    }
}