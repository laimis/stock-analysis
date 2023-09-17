using System;
using core.Shared;

namespace core.Cryptos
{
    internal class CryptoDeleted : AggregateEvent
    {
        public CryptoDeleted(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class CryptoPurchased : AggregateEvent, ICryptoTransaction
    {
        public CryptoPurchased(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            decimal quantity,
            decimal dollarAmountSpent,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Token = token;
            Quantity = quantity;
            DollarAmountSpent = dollarAmountSpent;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Token { get; }
        public decimal Quantity { get; }
        public decimal DollarAmountSpent { get; }
        public string Notes { get; }
        public decimal DollarAmount => DollarAmountSpent;
    }

    public class CryptoSold :
        AggregateEvent,
        ICryptoTransaction
    {
        public CryptoSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            decimal quantity,
            decimal dollarAmountReceived,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Token = token;
            Quantity = quantity;
            DollarAmountReceived = dollarAmountReceived;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Token { get; }
        public decimal Quantity { get; }
        public decimal DollarAmountReceived { get; }
        public string Notes { get; }

        public decimal DollarAmount => DollarAmountReceived;
    }

    public class CryptoAwarded :
        AggregateEvent,
        ICryptoTransaction
    {
        public CryptoAwarded(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            decimal quantity,
            decimal dollarAmountWorth,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Token = token;
            Quantity = quantity;
            DollarAmountWorth = dollarAmountWorth;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Token { get; }
        public decimal Quantity { get; }
        public decimal DollarAmountWorth { get; }
        public string Notes { get; }
        public decimal DollarAmount => DollarAmountWorth;
    }

    public class CryptoYielded :
        AggregateEvent,
        ICryptoTransaction
    {
        public CryptoYielded(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            decimal quantity,
            decimal dollarAmountWorth,
            string notes)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Token = token;
            Quantity = quantity;
            DollarAmountWorth = dollarAmountWorth;
            Notes = notes;
        }

        public Guid UserId { get; }
        public string Token { get; }
        public decimal Quantity { get; }
        public decimal DollarAmountWorth { get; }
        public string Notes { get; }
        public decimal DollarAmount => DollarAmountWorth;
    }

    internal class CryptoTransactionDeleted : AggregateEvent
    {
        public CryptoTransactionDeleted(
            Guid id,
            Guid aggregateId,
            Guid transactionId,
            DateTimeOffset when) : base(id, aggregateId, when)
        {
            TransactionId = transactionId;
        }

        public Guid TransactionId { get; }
    }

    internal class CryptoObtained : AggregateEvent
    {
        public CryptoObtained(Guid id, Guid aggregateId, DateTimeOffset when, string token, Guid userId) : base(id, aggregateId, when)
        {
            Token = token;
            UserId = userId;
        }

        public string Token { get; }
        public Guid UserId { get; }
    }
}