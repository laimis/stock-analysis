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

            note.Archive();

            _state = note.State;
        }

        private static Note CreateTestNote()
        {
            return new Note(
                            "userid",
                            "description",
                            "ticker",
                            100
                        );
        }

        [Fact]
        public void GuidAssigned()
        {
            Assert.NotNull(_state.Id);
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

        [Fact]
        public void Archived()
        {
            Assert.True(_state.IsArchived);
        }

        [Fact]
        public void UpdateWorks()
        {
            var note = CreateTestNote();

            note.Update("new note", null, null);

            Assert.Equal("new note", note.State.Note);
        }

        [Fact]
        public void UpdateArchivedFails()
        {
            var note = CreateTestNote();

            note.Archive();

            Assert.Throws<InvalidOperationException>(
                () => note.Update("new note", null, null)
            );
        }

        [Fact]
        public void DoubleArchiveNoOp()
        {
            var note = CreateTestNote();

            note.Archive();

            var eventCount = note.Events.Count;

            note.Archive();

            Assert.Equal(eventCount, note.Events.Count);
        }

        [Fact]
        public void ReminderSetting()
        {
            var note = CreateTestNote();

            Assert.False(note.State.HasReminder);

            note.SetupReminder(DateTimeOffset.UtcNow);

            Assert.True(note.State.HasReminder);

            note.ClearReminder();

            Assert.False(note.State.HasReminder);
        }
    }
}