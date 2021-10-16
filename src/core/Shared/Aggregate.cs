using System;
using System.Collections.Generic;

namespace core.Shared
{
    public abstract class Aggregate
    {
        public Aggregate()
        {
            _events = new List<AggregateEvent>();
        }

        public Aggregate(IEnumerable<AggregateEvent> events)
        {
            _events = new List<AggregateEvent>();
            Version = 0;
            foreach (var e in events)
            {
                Apply(e);
                Version++;
            }
        }

        public abstract IAggregateState AggregateState { get; }

        protected void Apply(AggregateEvent e)
        {
            _events.Add(e);

            AggregateState.Apply(e);
        }

        protected List<AggregateEvent> _events { get; }
        public IReadOnlyList<AggregateEvent> Events => _events.AsReadOnly();

        public int Version { get; }

        public Guid Id => AggregateState.Id;
    }
}