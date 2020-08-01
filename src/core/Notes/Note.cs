using System;
using System.Collections.Generic;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Notes
{
    public class Note : Aggregate
    {
        public NoteState State { get; } = new NoteState();
        public override IAggregateState AggregateState => State;

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
                    ticker
                )
            );
        }

        public bool MatchesTickerFilter(Ticker? filter)
        {
            if (filter == null)
            {
                return true;
            }

            return this.State.RelatedToTicker == filter.Value;
        }

        internal void Enrich(TickerPrice p, StockAdvancedStats d)
        {
            Apply(
                new NoteEnrichedWithPrice(
                    Guid.NewGuid(),
                    this.Id,
                    DateTimeOffset.UtcNow,
                    p,
                    d
                )
            );
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
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    note
                )
            );
        }
    }
}