using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Options;
using Xunit;

namespace coretests.Options
{
    public class ListTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public ListTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task List_WorksAsync()
        {
            var storage = _fixture.CreateStorageWithSoldOption();
            var opt = storage.SavedOptions.First();
            var query = new Stats.Query(null, false, Guid.NewGuid());

            var handler = new Stats.Handler(storage);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.Buy.Count);
        }
    }
}