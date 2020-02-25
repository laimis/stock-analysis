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
}