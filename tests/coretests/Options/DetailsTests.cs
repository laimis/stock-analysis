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
    public class DetailsTests : IClassFixture<OptionsTestsFixture>
    {
        private OptionsTestsFixture _fixture;

        public DetailsTests(OptionsTestsFixture fixture)
        {
            _fixture = fixture;
        }

        [Fact]
        public async Task Get_WorksAsync()
        {
            var storage = _fixture.CreateStorageWithSoldOption();
            var opt = storage.SavedOptions.First();
            var query = new Details.Query {
                Id = opt.State.Id
            };

            query.WithUserId(opt.State.UserId);

            var handler = new Details.Handler(storage, Mock.Of<IStocksService2>());

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}