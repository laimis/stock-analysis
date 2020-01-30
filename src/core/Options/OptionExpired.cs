using System;
using core.Shared;

namespace core.Options
{
    public class OptionExpired : AggregateEvent
    {
        public OptionExpired(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when)
            : base(id, aggregateId, when)
        {
        }
    }
}