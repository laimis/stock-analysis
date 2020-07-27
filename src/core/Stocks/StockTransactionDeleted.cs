using System;
using core.Shared;

namespace core.Stocks
{
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
}