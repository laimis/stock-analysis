using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Notes;
using Xunit;

namespace coretests.Notes
{
    public class ListTests : IClassFixture<NotesTestsFixture>
    {
        public ListTests(NotesTestsFixture fixture)
        {
            this.Fixture = fixture;
        }

        public NotesTestsFixture Fixture { get; }

        [Fact]
        public async Task ListSkipsArchived()
        {
            var handler = new core.Notes.List.Handler(Fixture.CreateStorageWithNotes());

            var query = new core.Notes.List.Query(NotesTestsFixture.UserId);

            var response = await handler.Handle(query, CancellationToken.None);

            Assert.Single(response.Notes);
        }
    }
}