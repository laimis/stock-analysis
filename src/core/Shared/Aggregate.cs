using System.Collections.Generic;

namespace core.Shared
{
    public class Aggregate
    {
        public Aggregate()
        {
            this._events = new List<AggregateEvent>();
        }

        public Aggregate(List<AggregateEvent> events) : this()
        {
            foreach (var e in events)
            {
                Apply(e);
            }

            this.Version = events.Count;
        }

        protected List<AggregateEvent> _events { get; }
        public IReadOnlyList<AggregateEvent> Events => _events.AsReadOnly();

        public int Version { get; }

        protected void Apply(AggregateEvent obj)
        {
            this._events.Add(obj);

            ApplyInternal(obj);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }
    }
}