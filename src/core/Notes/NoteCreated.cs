using System;
using core.Shared;

namespace core.Notes
{
    public class NoteCreated : AggregateEvent
    {
        public NoteCreated(Guid id, Guid aggregateId, DateTimeOffset when, string userId, string note, string ticker, double? predictedPrice)
            : base(id, aggregateId, when)
        {
            this.UserId = userId;
            this.Note = note;
            this.Ticker = ticker;
            this.PredictedPrice = predictedPrice;
        }

        public string UserId { get; }
        public string Note { get; }
        public string Ticker { get; }
        public double? PredictedPrice { get; }
    }
}