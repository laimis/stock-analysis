using System;

namespace core.Shared
{
    public class AggregateEvent
    {
        public AggregateEvent(Guid id, Guid aggregateId, DateTimeOffset when)
        {
            if (id == Guid.Empty)
            {
                throw new InvalidOperationException("id cannot be empty");
            }

            if (aggregateId == Guid.Empty)
            {
                throw new InvalidOperationException("aggregateId cannot be empty");
            }

            Id = id;
            AggregateId = aggregateId;
            When = when;
        }

        public Guid Id { get; }
        public Guid AggregateId { get; }
        public DateTimeOffset When { get; }
    }
}