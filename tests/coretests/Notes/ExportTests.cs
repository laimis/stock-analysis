using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace coretests.Notes
{
    public class ExportTests : IClassFixture<NotesTestsFixture>
    {
        public ExportTests(NotesTestsFixture fixture)
        {
            this.Fixture = fixture;
        }

        public NotesTestsFixture Fixture { get; }

        [Fact]
        public async Task ExportWorks()
        {
            var handler = new core.Notes.Export.Handler(Fixture.CreateStorageWithNotes());

            var query = new core.Notes.Export.Query(NotesTestsFixture.UserId);

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Contains("notes", response.Filename);
            Assert.Contains("\"multi line", response.Content);
            Assert.NotNull(response.Content);
            Assert.Contains(NotesTestsFixture.Ticker, response.Content);
        }
    }
}