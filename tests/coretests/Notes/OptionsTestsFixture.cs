using System;
using core.Notes;
using coretests.Fakes;

namespace coretests.Notes
{
    public class NotesTestsFixture
    {
        public const string Ticker = "ticker";
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
            note", Ticker, 100, DateTimeOffset.UtcNow);

            storage.Save(note, UserId);

            return storage;
        }
    }
}