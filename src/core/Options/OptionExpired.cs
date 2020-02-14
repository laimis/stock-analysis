using System;
using core.Shared;

namespace core.Options
{
    public class OptionExpired : AggregateEvent
    {
        public OptionExpired(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            bool assigned)
            : base(id, aggregateId, when)
        {
            this.Assigned = assigned;
        }

        public bool Assigned { get; }
    }
}