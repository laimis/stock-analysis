using System;
using core.Shared;

namespace core.Notes
{
    // NOTE: not used anymore, was thinking about a follow up concept
    // that turns out to be too complicated, keeping the event in to
    // make sure aggregate can be rebuilt
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