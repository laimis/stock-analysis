using System;
using core.Shared;
using MediatR;

namespace core.Crypto
{
    internal class CryptoDeleted : AggregateEvent
    {
        public CryptoDeleted(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class CryptoPurchased :
        AggregateEvent,
        INotification,
        ICryptoTransaction
    {
        public CryptoPurchased(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            double quantity,
            double dollarAmountSpent,
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
        public double Quantity { get; }
        public double DollarAmountSpent { get; }
        public string Notes { get; }
        public double DollarAmount => DollarAmountSpent;
    }

    public class CryptoSold :
        AggregateEvent,
        INotification,
        ICryptoTransaction
    {
        public CryptoSold(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            Guid userId,
            string token,
            double quantity,
            double dollarAmountReceived,
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
        public double Quantity { get; }
        public double DollarAmountReceived { get; }
        public string Notes { get; }

        public double DollarAmount => DollarAmountReceived;
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