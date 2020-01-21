using System;
using core.Shared;

namespace core.Notes
{
    public class NoteArchived : AggregateEvent
    {
        public NoteArchived(string ticker, DateTimeOffset when, string userId)
         : base(ticker, userId, when.DateTime)
        {
        }
    }
}