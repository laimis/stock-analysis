using System;
using core.Shared;

namespace core.Notes
{
    public class NoteUpdated : AggregateEvent
    {
        public NoteUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string note)
            : base(id, aggregateId, when)
        {
            this.Note = note;
        }

        public string Note { get; private set; }
    }
}