using System;
using core.Shared;

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

    [Obsolete("Use position labels instead")]
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

    public class StopPriceSet : AggregateEvent
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

    public class NotesAdded : AggregateEvent
    {
        public NotesAdded(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, int positionId, string notes) : base(id, aggregateId, when)
        {
            UserId = userId;
            PositionId = positionId;
            Notes = notes;
        }

        public Guid UserId { get; }
        public int PositionId { get; }
        public string Notes { get; }
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

    public class StopDeleted : AggregateEvent
    {
        public StopDeleted(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string ticker) : base(id, aggregateId, when)
        {
            UserId = userId;
            Ticker = ticker;
        }

        public Guid UserId { get; }
        public string Ticker { get; }
    }

    public class TradeGradeAssigned : AggregateEvent
    {
        public TradeGradeAssigned(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string grade, string note, int positionId) : base(id, aggregateId, when)
        {
            UserId = userId;
            Grade = grade;
            Note = note;
            PositionId = positionId;
        }

        public Guid UserId { get; }
        public int PositionId { get; }
        public string Grade { get; }
        public string Note { get; }
    }

    public class PositionDeleted : AggregateEvent
    {
        public PositionDeleted(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, int positionId) : base(id, aggregateId, when)
        {
            PositionId = positionId;
            UserId = userId;
        }
        public int PositionId { get; }
        public Guid UserId { get; }
    }

    public class PositionLabelSet : AggregateEvent
    {
        public PositionLabelSet(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, int positionId, string key, string value) :
            base(id, aggregateId, when)
        {
            UserId = userId;
            PositionId = positionId;
            Key = key;
            Value = value;
        }

        public Guid UserId { get; }
        public int PositionId { get; }
        public string Key { get; }
        public string Value { get; }
    }

    public class PositionLabelDeleted : AggregateEvent
    {
        public PositionLabelDeleted(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, int positionId, string key) :
            base(id, aggregateId, when)
        {
            UserId = userId;
            PositionId = positionId;
            Key = key;
        }

        public Guid UserId { get; }
        public int PositionId { get; }
        public string Key { get; }
    }

    public class PositionRiskAmountSet : AggregateEvent
    {
        public PositionRiskAmountSet(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, int positionId, decimal riskAmount) :
            base(id, aggregateId, when)
        {
            UserId = userId;
            PositionId = positionId;
            RiskAmount = riskAmount;
        }

        public Guid UserId { get; }
        public int PositionId { get; }
        public decimal RiskAmount { get; }
    }
}