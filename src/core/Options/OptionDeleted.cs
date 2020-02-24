using System;
using core.Shared;

namespace core.Options
{
    public class OptionDeleted : AggregateEvent
    {
        public OptionDeleted(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}