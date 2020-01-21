using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Notes
{
    public class Note : Aggregate
    {
        private NoteState _state = new NoteState();
        public NoteState State => _state;

        public Note(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public Note(string userId, string note, string ticker, double? predictedPrice)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (predictedPrice != null && predictedPrice.Value < 0)
            {
                throw new InvalidOperationException("Predicted price cannot be negative");
            }

            if (string.IsNullOrWhiteSpace(note))
            {
                throw new InvalidOperationException("Note cannot be empty");
            }

            Apply(
                new NoteCreated(
                    Guid.NewGuid().ToString(),
                    DateTimeOffset.UtcNow,
                    userId,
                    note,
                    ticker,
                    predictedPrice)
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
            this.State.Id = created.Ticker;
            this.State.RelatedToTicker = created.RelatedToTicker;
            this.State.Created = created.When;
            this.State.Note = created.Note;
            this.State.PredictedPrice = created.PredictedPrice;
        }
    }
}