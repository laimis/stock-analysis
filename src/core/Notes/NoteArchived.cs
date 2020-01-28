using System;
using core.Shared;

namespace core.Notes
{
    public class NoteArchived : AggregateEvent
    {
        public NoteArchived(Guid id, Guid aggregateId, DateTimeOffset when)
         : base(id, aggregateId, when.DateTime)
        {
        }
    }
}