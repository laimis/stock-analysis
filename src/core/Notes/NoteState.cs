using System;
using core.Adapters.Stocks;

namespace core.Notes
{
    public class NoteState
    {
        public Guid Id { get; internal set; }
        public string RelatedToTicker { get; internal set; }
        public DateTimeOffset Created { get; internal set; }
        public string Note { get; internal set; }
        public Guid UserId { get; internal set; }
        public DateTimeOffset StatsApplied { get; private set; }
        public StockAdvancedStats Stats { get; private set; }
        public TickerPrice Price { get; private set; }

        internal void Apply(NoteEnriched enriched)
        {
            this.StatsApplied = enriched.When;
            this.Stats = enriched.Stats;
        }

        internal void Apply(NoteEnrichedWithPrice enriched)
        {
            this.StatsApplied = enriched.When;
            this.Stats = enriched.Stats;
            this.Price = enriched.Price;
        }

        internal void Apply(NoteUpdated updated)
        {
            this.Note = updated.Note;
        }

        internal void Apply(NoteCreated c)
        {
            this.Id = c.AggregateId;
            this.UserId = c.UserId;
            this.RelatedToTicker = c.Ticker;
            this.Created = c.When;
            this.Note = c.Note;
        }
    }
}