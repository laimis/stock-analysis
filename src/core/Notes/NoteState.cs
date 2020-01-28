using System;

namespace core.Notes
{
    public class NoteState
    {
        public Guid Id { get; internal set; }
        public string RelatedToTicker { get; internal set; }
        public DateTimeOffset Created { get; internal set; }
        public string Note { get; internal set; }
        public double? PredictedPrice { get; internal set; }
        public string UserId { get; internal set; }
    }
}