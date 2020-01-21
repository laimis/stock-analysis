using System;

namespace core.Notes
{
    public class NoteState
    {
        public string Id { get; internal set; }
        public string RelatedToTicker { get; internal set; }
        public DateTime Created { get; internal set; }
        public string Note { get; internal set; }
        public double? PredictedPrice { get; internal set; }
    }
}