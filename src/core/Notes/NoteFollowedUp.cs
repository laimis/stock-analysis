using System;
using core.Shared;

namespace core.Notes
{
    public class NoteFollowedUp : AggregateEvent
    {
        public NoteFollowedUp(string ticker, DateTimeOffset when, string userId, string text)
            : base(ticker, userId, when.DateTime)
        {
            this.Text = text;
        }

        public string Text { get; }
    }
}