using System;
using core.Shared;

namespace core.Notes
{
    public class NoteUpdated : AggregateEvent
    {
        public NoteUpdated(string ticker, DateTimeOffset when, string userId, string note, double? predictedPrice)
            : base(ticker, userId, when.DateTime)
        {
            this.Note = note;
            this.PredictedPrice = predictedPrice;
        }

        public string Note { get; private set; }
        public double? PredictedPrice { get; }
    }
}