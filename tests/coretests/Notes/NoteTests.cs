using System;
using core.Notes;
using core.Shared;
using coretests.testdata;
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
                TestDataGenerator.AMD,
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
            Assert.Equal(TestDataGenerator.AMD, _state.RelatedToTicker);
        }

        [Fact]
        public void FailWithEmptyUser()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.Empty, "some note", TestDataGenerator.AMD, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithNoNote()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.NewGuid(), "", TestDataGenerator.AMD, DateTimeOffset.UtcNow));
        }

        [Fact]
        public void FailWithFutureCreated()
        {
            Assert.Throws<InvalidOperationException>(() => new Note(Guid.NewGuid(), "note", TestDataGenerator.AMD, DateTimeOffset.UtcNow.AddDays(1)));
        }

        [Fact]
        public void UpdateWorks()
        {
            var note = CreateTestNote();

            note.Update("new note");

            Assert.Equal("new note", note.State.Note);
        }

        [Fact]
        public void MatchesFilterWithNullWorks()
        {
            var note = CreateTestNote();

            Assert.True(note.MatchesTickerFilter(null));
        }

        [Fact]
        public void MatchesFilterWithMatchingText()
        {
            var note = CreateTestNote();

            Assert.True(note.MatchesTickerFilter(note.State.RelatedToTicker));
        }
    }
}