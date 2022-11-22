using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using Moq;
using Xunit;

namespace coretests.Stocks.Services
{
    public class TradeStrategyRunnerTests
    {
        [Fact]
        public async Task RunAsync()
        {
            var prices = new List<PriceBar>();
            for(var i = 0; i < 100; i++)
            {
                prices.Add(
                    new PriceBar(
                        date: System.DateTimeOffset.UtcNow.AddDays(i),
                        open: 10+i, high: 10+i,
                        low: 10+i, close: 10+i,
                        volume: 1000
                    )
                );
            }

            var mock = new Mock<IBrokerage>();
            mock.Setup(x => x.GetPriceHistory(It.IsAny<UserState>(), It.IsAny<string>(), It.IsAny<PriceFrequency>(), It.IsAny<System.DateTimeOffset>(), It.IsAny<System.DateTimeOffset>()))
                .ReturnsAsync(new ServiceResponse<PriceBar[]>(prices.ToArray()));

            var runner = new TradeStrategyRunner(mock.Object);
            var result = await runner.RunAsync(
                new UserState(),
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            Assert.True(result.IsClosed);
            Assert.Equal(1005, result.Profit);
            Assert.Equal(100.50m, result.GainPct);
        }
    }
}