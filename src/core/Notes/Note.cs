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

        public void ClearReminder()
        {
            if (!this.State.HasReminder)
            {
                return;
            }

            Apply(
                new NoteReminderCleared(
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId
                )
            );
        }

        public void SetupReminder(DateTimeOffset reminderDate)
        {
            Apply(
                new NoteReminderSet(
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    reminderDate
                )
            );
        }

        public void Archive()
        {
            if (!this.State.IsArchived)
            {
                Apply(
                    new NoteArchived(
                        this.State.Id,
                        DateTimeOffset.UtcNow,
                        this.State.UserId
                    )
                );
            }
        }

        public void Update(string note, string ticker, double? predictedPrice)
        {
            if (this.State.IsArchived)
            {
                throw new InvalidOperationException("Archived note cannot be updated");
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
                new NoteUpdated(
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    note,
                    ticker,
                    predictedPrice
                )
            );
        }

        public void Followup(string text)
        {
            Apply(
                new NoteFollowedUp(
                    this.State.Id,
                    DateTimeOffset.UtcNow,
                    this.State.UserId,
                    text
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
            this.State.Id = created.Ticker;
            this.State.UserId = created.UserId;
            this.State.RelatedToTicker = created.RelatedToTicker;
            this.State.Created = created.When;
            this.State.Note = created.Note;
            this.State.PredictedPrice = created.PredictedPrice;
        }

        protected void ApplyInternal(NoteUpdated updated)
        {
            this.State.RelatedToTicker = updated.RelatedToTicker;
            this.State.Note = updated.Note;
            this.State.PredictedPrice = updated.PredictedPrice;
        }

        protected void ApplyInternal(NoteArchived archived)
        {
            this.State.IsArchived = true;
        }

        protected void ApplyInternal(NoteReminderCleared cleared)
        {
            this.State.ReminderDate = null;
        }

        protected void ApplyInternal(NoteReminderSet set)
        {
            this.State.ReminderDate = set.ReminderDate;
        }

        protected void ApplyInternal(NoteFollowedUp e)
        {
            this.State.AddFollowup(e.When, e.Text);
        }
    }
}