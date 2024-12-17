using System;
using core.Shared;

namespace core.Stocks
{
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
    
    internal class PendingStockPositionCreatedWithStrategyAndSizeStop : AggregateEvent
    {
        public PendingStockPositionCreatedWithStrategyAndSizeStop(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string notes,
            decimal numberOfShares,
            decimal price,
            decimal? stopPrice,
            decimal? sizeStopPrice,
            string strategy,
            string ticker,
            Guid userId)
            : base(id, aggregateId, when)
        {
            Notes = notes;
            NumberOfShares = numberOfShares;
            Price = price;
            StopPrice = stopPrice;
            SizeStopPrice = sizeStopPrice;
            Strategy = strategy;
            Ticker = ticker;
            UserId = userId;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal Price { get; }
        public decimal NumberOfShares { get; }
        public decimal? StopPrice { get; }
        public decimal? SizeStopPrice { get; }
        public string Notes { get; }
        public string Strategy { get; }
    }
    
    public class PendingStockPositionRealized : AggregateEvent
    {
        public PendingStockPositionRealized(Guid id, Guid aggregateId, DateTimeOffset when, decimal price)
            : base(id, aggregateId, when)
        {
            Price = price;
        }

        public decimal Price { get; }
    }

    public class PendingStockPositionOrderDetailsAdded : AggregateEvent
    {
        public PendingStockPositionOrderDetailsAdded(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string orderType,
            string orderDuration)
            : base(id, aggregateId, when)
        {
            OrderType = orderType;
            OrderDuration = orderDuration;
        }
        
        public string OrderType { get; }
        public string OrderDuration { get; }
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
    
    public class PendingStockPositionClosedWithReason : AggregateEvent
    {
        public PendingStockPositionClosedWithReason(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string reason)
            : base(id, aggregateId, when)
        {
            Reason = reason;
        }

        public string Reason { get; }
    }
}
