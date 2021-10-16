using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace coretests.Notes
{
    public class ListTests : IClassFixture<NotesTestsFixture>
    {
        public ListTests(NotesTestsFixture fixture)
        {
            Fixture = fixture;
        }

        public NotesTestsFixture Fixture { get; }

        [Fact]
        public async Task ListWorks()
        {
            var handler = new core.Notes.List.Handler(Fixture.CreateStorageWithNotes());

            var query = new core.Notes.List.Query(NotesTestsFixture.UserId, null);

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Single(response.Notes);
        }

        [Fact]
        public async Task ListFilterWorks()
        {
            var handler = new core.Notes.List.Handler(Fixture.CreateStorageWithNotes());

            var query = new core.Notes.List.Query(NotesTestsFixture.UserId, "filtered");

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Empty(response.Notes);

            query = new core.Notes.List.Query(NotesTestsFixture.UserId, NotesTestsFixture.Ticker);

            response = await handler.Handle(query, CancellationToken.None);

            Assert.Single(response.Notes);
        }
    }
}