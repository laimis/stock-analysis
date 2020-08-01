using System;

namespace core.Shared
{
    public interface IAggregateState
    {
        Guid Id { get; }
        void Apply(AggregateEvent e);
    }
}