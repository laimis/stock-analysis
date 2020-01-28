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
            var note = CreateTestNote();

            _state = note.State;
        }

        private static Note CreateTestNote()
        {
            return new Note(
                            "userid",
                            "description",
                            "ticker",
                            100,
                            DateTimeOffset.UtcNow
                        );
        }

        [Fact]
        public void GuidAssigned()
        {
            Assert.NotEqual(Guid.Empty, _state.Id);
        }

        [Fact]
        public void DateAssigned()
        {
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
            Assert.Throws<InvalidOperationException>(() => new Note(null, "some note", "ticker", null, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithNegativePrediction()
        {
            Assert.Throws<InvalidOperationException>(() => new Note("user", "some note", "ticker", -12, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithNoNote()
        {
            Assert.Throws<InvalidOperationException>(() => new Note("user", "", "ticker", 12, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithFutureCreated()
        {
            Assert.Throws<InvalidOperationException>(() => new Note("user", "note", "ticker", 12, DateTimeOffset.UtcNow.AddDays(1)));
        }

        [Fact]
        public void UpdateWorks()
        {
            var note = CreateTestNote();

            note.Update("new note", null);

            Assert.Equal("new note", note.State.Note);
        }
    }
}