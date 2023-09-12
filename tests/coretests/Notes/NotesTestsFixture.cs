using System;
using core;
using core.Notes;
using core.Shared;
using core.Shared.Adapters.Storage;
using Moq;

namespace coretests.Notes
{
    public class NotesTestsFixture
    {
        public static readonly Ticker Ticker = "tsla";
        public static Guid UserId = Guid.NewGuid();

        public IPortfolioStorage CreateStorageWithNotes()
        {
            var note = new Note(UserId, @"multi line
            note", Ticker, DateTimeOffset.UtcNow);

            var mock = new Mock<IPortfolioStorage>();
            mock.Setup(x => x.GetNotes(UserId))
                .ReturnsAsync(new[] { note });
            return mock.Object;
        }
    }
}