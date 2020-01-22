using System;
using core.Shared;

namespace core.Notes
{
    public class NoteReminderSet : AggregateEvent
    {
        public NoteReminderSet(string ticker, DateTimeOffset when, string userId, DateTimeOffset reminderDate)
         : base(ticker, userId, when.DateTime)
        {
            this.ReminderDate = reminderDate;
        }

        public DateTimeOffset ReminderDate { get; }
    }
}