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
                Guid.NewGuid(),
                "description",
                "ticker",
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
        public void FailWithEmptyUser()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.Empty, "some note", "ticker", DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithNoNote()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.NewGuid(), "", "ticker", DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithFutureCreated()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.NewGuid(), "note", "ticker", DateTimeOffset.UtcNow.AddDays(1)));
        }

        [Fact]
        public void UpdateWorks()
        {
            var note = CreateTestNote();

            note.Update("new note");

            Assert.Equal("new note", note.State.Note);
        }
    }
}