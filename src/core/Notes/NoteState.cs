using System;
using core.Shared;

namespace core.Notes
{
    public class NoteState : IAggregateState
    {
        public Guid Id { get; internal set; }
        public Ticker RelatedToTicker { get; internal set; }
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
        
        internal void ApplyInternal(NoteUpdated updated)
        {
            Note = updated.Note;
        }

        internal void ApplyInternal(NoteCreated c)
        {
            Id = c.AggregateId;
            UserId = c.UserId;
            RelatedToTicker = new Ticker(c.Ticker);
            Created = c.When;
            Note = c.Note;
        }
    }
}