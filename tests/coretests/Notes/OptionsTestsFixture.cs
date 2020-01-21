using core.Notes;
using coretests.Fakes;

namespace coretests.Notes
{
    public class NotesTestsFixture
    {
        public const string Ticker = "ticker";
        public const string UserId = "userid";

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
            note", Ticker, 100);

            storage.Save(note);

            return storage;
        }
    }
}