using System;
using core.Shared;

namespace core.Notes
{
    public class NoteUpdated : AggregateEvent
    {
        public NoteUpdated(Guid id, Guid aggregateId, DateTimeOffset when, string note, double? predictedPrice)
            : base(id, aggregateId, when)
        {
            this.Note = note;
            this.PredictedPrice = predictedPrice;
        }

        public string Note { get; private set; }
        public double? PredictedPrice { get; }
    }
}