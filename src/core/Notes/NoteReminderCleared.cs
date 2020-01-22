using System;
using core.Shared;

namespace core.Notes
{
    public class NoteReminderCleared : AggregateEvent
    {
        public NoteReminderCleared(string ticker, DateTimeOffset when, string userId)
         : base(ticker, userId, when.DateTime)
        {
        }
    }
}