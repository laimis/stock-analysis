using System;
using core.Shared;

namespace core.Notes
{
    public class NoteCreated : AggregateEvent
    {
        public NoteCreated(string ticker, DateTimeOffset when, string userId, string note, string relatedToTicker, double? predictedPrice)
            : base(ticker, userId, when.DateTime)
        {
            this.Note = note;
            this.RelatedToTicker = relatedToTicker;
            this.PredictedPrice = predictedPrice;
        }

        public string Note { get; private set; }
        public string RelatedToTicker { get; }
        public double? PredictedPrice { get; }
    }
}