using System;
using core.Shared;

namespace core.Notes
{
    public class NoteCreated : AggregateEvent
    {
        public NoteCreated(Guid id, Guid aggregateId, DateTimeOffset when, Guid userId, string note, string ticker)
            : base(id, aggregateId, when)
        {
            UserId = userId;
            Note = note;
            Ticker = ticker;
        }

        public Guid UserId { get; }
        public string Note { get; }
        public string Ticker { get; }
    }

    public class NoteUpdated : AggregateEvent
    {
        public NoteUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string note)
            : base(id, aggregateId, when)
        {
            Note = note;
        }

        public string Note { get; private set; }
    }
}