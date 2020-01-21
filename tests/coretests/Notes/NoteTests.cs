using System;
using core.Notes;
using Xunit;

namespace coretests.Notes
{
    public class NoteTests
    {
        private NoteState _state;

        public NoteTests()
        {
            var note = new Note(
                "userid",
                "description",
                "ticker",
                100
            );

            _state = note.State;
        }

        [Fact]
        public void GuidAssigned()
        {
            Assert.NotNull(_state.Id);
        }

        [Fact]
        public void DateAssigned()
        {
            Assert.NotNull(_state.Created);
            Assert.NotEqual(DateTime.MinValue, _state.Created);
        }

        [Fact]
        public void Description()
        {
            Assert.Equal("description", _state.Note);
        }

        [Fact]
        public void Ticker()
        {
            Assert.Equal("ticker", _state.RelatedToTicker);
        }

        [Fact]
        public void Prediction()
        {
            Assert.Equal(100, _state.PredictedPrice);
        }

        [Fact]
        public void FailWithEmptyUser()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(null, "some note", null, null));
        }

        [Fact]
        public void FailWithNegativePrediction()
        {
            Assert.Throws<InvalidOperationException>(() => new Note("user", "some note", null, -12));
        }

        [Fact]
        public void FailWithNoNote()
        {
            Assert.Throws<InvalidOperationException>(() => new Note("user", "", null, 12));
        }
    }
}