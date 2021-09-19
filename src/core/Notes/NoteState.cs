using System;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Notes
{
    public class NoteState : IAggregateState
    {
        public Guid Id { get; internal set; }
        public string RelatedToTicker { get; internal set; }
        public DateTimeOffset Created { get; internal set; }
        public string Note { get; internal set; }
        public Guid UserId { get; internal set; }
        public DateTimeOffset StatsApplied { get; private set; }
        public StockAdvancedStats Stats { get; private set; }
        public Price Price { get; private set; }

        public void Apply(AggregateEvent e)
        {
            this.ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }
        
        protected void ApplyInternal(NoteArchived archived)
        {
        }
        protected void ApplyInternal(NoteReminderCleared cleared)
        {
        }
        protected void ApplyInternal(NoteReminderSet set)
        {
        }
        protected void ApplyInternal(NoteFollowedUp e)
        {
        }

        internal void ApplyInternal(NoteEnriched enriched)
        {
            this.StatsApplied = enriched.When;
            this.Stats = enriched.Stats;
        }

        internal void ApplyInternal(NoteEnrichedWithPrice enriched)
        {
            this.StatsApplied = enriched.When;
            this.Stats = enriched.Stats;
            this.Price = enriched.Price;
        }

        internal void ApplyInternal(NoteUpdated updated)
        {
            this.Note = updated.Note;
        }

        internal void ApplyInternal(NoteCreated c)
        {
            this.Id = c.AggregateId;
            this.UserId = c.UserId;
            this.RelatedToTicker = c.Ticker;
            this.Created = c.When;
            this.Note = c.Note;
        }
    }
}