using System;
using core.Shared;

namespace core.Notes
{
    // NOTE: not used anymore, was thinking about a follow up concept
    // that turns out to be too complicated, keeping the event in to
    // make sure aggregate can be rebuilt
    public class NoteFollowedUp : AggregateEvent
    {
        public NoteFollowedUp(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}