using System;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Stocks;
using core.Stocks.View;
using storage.postgres;
using storage.shared;
using storage.tests;
using testutils;
using Xunit;

namespace storagetests.postgres
{
    [Trait("Category", "Postgres")]
    public class PostgresPortfolioStorageTests : PortfolioStorageTests
    {
        protected string _cnn = null;

        public PostgresPortfolioStorageTests()
        {
            _cnn = CredsHelper.GetDbCreds();
        }

        

        [Fact]
        public async Task EndToEnd()
        {
            var storage = new AccountStorage(
                new Fakes.FakeMediator(),
                _cnn
            );

            var user = await storage.GetUserByEmail("laimis@gmail.com");

            var portfolioStorage = CreateStorage();
            var stocks = await portfolioStorage.GetStocks(user.Id);

            var closedPositions = stocks
                    .SelectMany(s => s.State.PositionInstances.Where(t => t.IsClosed))
                    .OrderByDescending(p => p.Closed)
                    .ToArray();

            var performance = new TradingPerformanceContainerView(
                    new Span<PositionInstance>(closedPositions),
                    20
                );

            Assert.True(performance.Recent.WinPct == 35);
        }

        protected override IPortfolioStorage CreateStorage() =>
            new PortfolioStorage(
                new PostgresAggregateStorage(new Fakes.FakeMediator(), _cnn),
                null
            );
    }
}