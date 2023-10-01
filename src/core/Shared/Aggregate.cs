using System;
using System.Collections.Generic;

namespace core.Shared
{
    public interface IAggregate
    {
        int Version { get; }
        IEnumerable<AggregateEvent> Events { get; }
    }
    
    public abstract class Aggregate<T> : IAggregate where T : IAggregateState, new()
    {
        protected Aggregate()
        {
            _aggregateState = new ();
            _events = new List<AggregateEvent>();
            Version = 0;
        }
        
        protected Aggregate(IEnumerable<AggregateEvent> events) : this()
        {
            foreach (var e in events)
            {
                Apply(e);
                Version++;
            }
        }

        protected void Apply(AggregateEvent e)
        {
            _events.Add(e);
            _aggregateState.Apply(e);
        }

        private readonly T _aggregateState;
        public T State => _aggregateState;
        
        private readonly List<AggregateEvent> _events;
        public IEnumerable<AggregateEvent> Events => _events;

        public int Version { get; }
        public Guid Id => _aggregateState.Id;
    }
}