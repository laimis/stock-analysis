using System;
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
        public Price Price { get; private set; }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
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

        [Obsolete]
        internal void ApplyInternal(NoteEnriched enriched)
        {
            StatsApplied = enriched.When;
        }

        [Obsolete]
        internal void ApplyInternal(NoteEnrichedWithPrice enriched)
        {
            StatsApplied = enriched.When;
        }

        internal void ApplyInternal(NoteUpdated updated)
        {
            Note = updated.Note;
        }

        internal void ApplyInternal(NoteCreated c)
        {
            Id = c.AggregateId;
            UserId = c.UserId;
            RelatedToTicker = c.Ticker;
            Created = c.When;
            Note = c.Note;
        }
    }
}