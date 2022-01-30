using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Options;
using Moq;
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
            var query = new Dashboard.Query(Guid.NewGuid());

            var handler = new Dashboard.Handler(storage, Mock.Of<IStocksService2>());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.Buy.Count);
        }
    }
}