using System;
using core.Shared;

namespace core.Notes
{
    public class NoteReminderSet : AggregateEvent
    {
        public NoteReminderSet(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}