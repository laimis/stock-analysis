using System;
using core.Shared;

namespace core.Notes
{
    public class NoteReminderCleared : AggregateEvent
    {
        public NoteReminderCleared(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}