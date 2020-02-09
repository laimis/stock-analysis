using System;
using System.Collections.Generic;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Notes
{
    public class Note : Aggregate
    {
        private NoteState _state = new NoteState();
        public NoteState State => _state;
        public override Guid Id => State.Id;

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

        protected override void Apply(AggregateEvent e)
        {
            this._events.Add(e);

            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }

        protected void ApplyInternal(NoteCreated created)
        {
            this.State.Apply(created);
        }

        protected void ApplyInternal(NoteUpdated updated)
        {
            this.State.Apply(updated);
        }

        protected void ApplyInternal(NoteEnriched enriched)
        {
            this.State.Apply(enriched);
        }

        protected void ApplyInternal(NoteEnrichedWithPrice enriched)
        {
            this.State.Apply(enriched);
        }

        // these are no longer used
        protected void ApplyInternal(NoteArchived archived)
        {
        }
        protected void ApplyInternal(NoteReminderCleared cleared)
        {
        }
        protected void ApplyInternal(NoteReminderSet set)
        {
        }
        protected void ApplyInternal(NoteFollowedUp e)
        {
        }
    }
}