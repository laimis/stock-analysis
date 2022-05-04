﻿using System;
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
            var (storage, opt) = _fixture.CreateStorageWithSoldOption();
            
            var query = new Details.Query {
                Id = opt.State.Id
            };

            var mock = new Mock<IStocksService2>();
            mock.Setup(x => x.GetPrice(opt.State.Ticker))
                .Returns(Task.FromResult(
                    new StockServiceResponse<core.Price>(
                        new core.Price(100)
                    )
                ));

            query.WithUserId(opt.State.UserId);

            var handler = new Details.Handler(storage, mock.Object);

            var result = await handler.Handle(query, CancellationToken.None);

            Assert.NotNull(result);
        }
    }
}