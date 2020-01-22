using System;
using core.Shared;

namespace core.Notes
{
    public class NoteFollowedUp : AggregateEvent
    {
        public NoteFollowedUp(string ticker, DateTimeOffset utcNow, string userId, string text)
            : base(ticker, userId, utcNow.DateTime)
        {
            this.Text = text;
        }

        public string Text { get; }
    }
}