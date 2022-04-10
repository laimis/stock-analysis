using System;
using System.Collections.Generic;
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
            var (storage, _) = _fixture.CreateStorageWithSoldOption();
            
            var query = new Dashboard.Query(Guid.NewGuid());

            var mock = new Mock<IStocksService2>();
            mock.Setup(x => x.GetPrices(It.IsAny<IEnumerable<string>>()))
                .Returns(Task.FromResult(
                    new StockServiceResponse<Dictionary<string, BatchStockPrice>, string>(
                        new Dictionary<string, BatchStockPrice>()
                    )
                ));
            
            var handler = new Dashboard.Handler(storage, mock.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.Equal(0, result.Buy.Count);
        }
    }
}