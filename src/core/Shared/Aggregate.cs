using System;
using System.Collections.Generic;

namespace core.Shared
{
    public abstract class Aggregate
    {
        public Aggregate()
        {
            this._events = new List<AggregateEvent>();
        }

        public Aggregate(IEnumerable<AggregateEvent> events)
        {
            this._events = new List<AggregateEvent>();
            this.Version = 0;
            foreach (var e in events)
            {
                Apply(e);
                this.Version++;
            }
        }

        public abstract IAggregateState AggregateState { get; }

        protected void Apply(AggregateEvent e)
        {
            this._events.Add(e);

            AggregateState.Apply(e);
        }

        protected List<AggregateEvent> _events { get; }
        public IReadOnlyList<AggregateEvent> Events => _events.AsReadOnly();

        public int Version { get; }

        public Guid Id => AggregateState.Id;
    }
}