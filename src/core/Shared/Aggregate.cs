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

        public Aggregate(List<AggregateEvent> events)
        {
            this._events = new List<AggregateEvent>();
            
            foreach (var e in events)
            {
                Apply(e);
            }

            this.Version = events.Count;
        }

        protected abstract void Apply(AggregateEvent e);

        protected List<AggregateEvent> _events { get; }
        public IReadOnlyList<AggregateEvent> Events => _events.AsReadOnly();

        public int Version { get; }
    }
}