using System;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Notes
{
    public class NoteEnrichedWithPrice : AggregateEvent
    {
        public NoteEnrichedWithPrice(Guid id, Guid aggregateId, DateTimeOffset when, TickerPrice price, StockAdvancedStats stats)
            : base(id, aggregateId, when)
        {
            this.Price = price;
            this.Stats = stats;
        }

        public TickerPrice Price { get; }
        public StockAdvancedStats Stats { get; }
    }
}