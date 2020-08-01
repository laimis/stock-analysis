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
            double price,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.Ticker = ticker;
            this.NumberOfShares = numberOfShares;
            this.Price = price;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public int NumberOfShares { get; }
        public double Price { get; }
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
            double price,
            string notes)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.Ticker = ticker;
            this.NumberOfShares = numberOfShares;
            this.Price = price;
            this.Notes = notes;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
        public int NumberOfShares { get; }
        public double Price { get; }
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

    internal class TickerObtained : AggregateEvent
    {
        public TickerObtained(Guid id, Guid aggregateId, DateTimeOffset when, string ticker, Guid userId) : base(id, aggregateId, when)
        {
            this.Ticker = ticker;
            this.UserId = userId;
        }

        public string Ticker { get; }
        public Guid UserId { get; }
    }
}