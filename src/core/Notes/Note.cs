using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Notes
{
    public class Note : Aggregate<NoteState>
    {
        public Note(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public Note(Guid userId, string note, Ticker ticker, DateTimeOffset created)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                throw new InvalidOperationException("Note cannot be empty");
            }

            if (created > DateTimeOffset.UtcNow)
            {
                throw new InvalidOperationException("Note creation date cannot be in the future");
            }

            Apply(
                new NoteCreated(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    created,
                    userId,
                    note,
                    ticker.Value
                )
            );
        }

        public bool MatchesTickerFilter(Ticker? filter)
        {
            if (filter == null)
            {
                return true;
            }

            return State.RelatedToTicker.Equals(filter.Value);
        }

        public void Update(string note)
        {
            if (string.IsNullOrWhiteSpace(note))
            {
                throw new InvalidOperationException("Note cannot be empty");
            }

            Apply(
                new NoteUpdated(
                    Guid.NewGuid(),
                    State.Id,
                    DateTimeOffset.UtcNow,
                    note
                )
            );
        }
    }
}