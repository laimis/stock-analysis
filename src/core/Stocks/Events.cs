using System;
using core.Shared;
using MediatR;

namespace core.Stocks
{
    internal class StockDeleted : AggregateEvent
    {
        public StockDeleted(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class StockPurchased : 
        AggregateEvent,
        INotification,
        IStockTransaction
    {
        public StockPurchased(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string ticker,
            decimal numberOfShares,
            decimal price,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            NumberOfShares = numberOfShares;
            Price = price;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal NumberOfShares { get; }
        public decimal Price { get; }
        public string Notes { get; }
    }

    public class StockPurchased_v2 : 
        AggregateEvent,
        INotification,
        IStockTransactionWithStopPrice
    {
        public StockPurchased_v2(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string ticker,
            decimal numberOfShares,
            decimal price,
            string notes,
            decimal? stopPrice)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            NumberOfShares = numberOfShares;
            Price = price;
            Notes = notes;
            StopPrice = stopPrice;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal NumberOfShares { get; }
        public decimal Price { get; }
        public string Notes { get; }
        public decimal? StopPrice { get; }
    }

    public class StockSold :
        AggregateEvent,
        INotification,
        IStockTransaction
    {
        public StockSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string ticker,
            decimal numberOfShares,
            decimal price,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            NumberOfShares = numberOfShares;
            Price = price;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal NumberOfShares { get; }
        public decimal Price { get; }
        public string Notes { get; }
    }

    internal class StockTransactionDeleted : AggregateEvent
    {
        public StockTransactionDeleted(
            Guid id,
            Guid aggregateId,
            Guid transactionId,
            DateTimeOffset when) : base(id, aggregateId, when)
        {
            TransactionId = transactionId;
        }

        public Guid TransactionId { get; }
    }

    internal class StockCategoryChanged : AggregateEvent
    {
        public StockCategoryChanged(Guid id, Guid aggregateId, string category, DateTimeOffset when) : base(id, aggregateId, when)
        {
            Category =category;
        }

        public string Category { get; }
    }

    internal class TickerObtained : AggregateEvent
    {
        public TickerObtained(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, Guid userId) : base(id, aggregateId, when)
        {
            Ticker = ticker;
            UserId = userId;
        }

        public string Ticker { get; }
        public Guid UserId { get; }
    }

    public class StopPriceSet : AggregateEvent, INotification
    {
        public StopPriceSet(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string ticker, decimal stopPrice) : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            StopPrice = stopPrice;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal StopPrice { get; }
    }

    internal class RiskAmountSet : AggregateEvent
    {
        public RiskAmountSet(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string ticker, decimal riskAmount) : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
            RiskAmount = riskAmount;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public decimal RiskAmount { get; }
    }
}