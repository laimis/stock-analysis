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
            int numberOfShares,
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
        public int NumberOfShares { get; }
        public decimal Price { get; }
        public string Notes { get; }
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
            int numberOfShares,
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
        public int NumberOfShares { get; }
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
}