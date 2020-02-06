using System;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Notes
{
    public class NoteEnriched : AggregateEvent
    {
        public NoteEnriched(Guid id, Guid aggregateId, DateTimeOffset when, StockAdvancedStats stats)
            : base(id, aggregateId, when)
        {
            this.Stats = stats;
        }

        public StockAdvancedStats Stats { get; }
    }
}