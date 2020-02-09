using System;
using core.Notes;
using core.Shared;
using coretests.Fakes;

namespace coretests.Notes
{
    public class NotesTestsFixture
    {
        public static readonly Ticker Ticker = "tsla";
        public static Guid UserId = Guid.NewGuid();

        public NotesTestsFixture()
        {
        }

        public FakePortfolioStorage CreateStorageWithNotes()
        {
            return CreateStorage();
        }

        private FakePortfolioStorage CreateStorage()
        {
            var storage = new FakePortfolioStorage();

            var note = new Note(UserId, @"multi line
            note", Ticker, DateTimeOffset.UtcNow);

            storage.Save(note, UserId);

            return storage;
        }
    }
}