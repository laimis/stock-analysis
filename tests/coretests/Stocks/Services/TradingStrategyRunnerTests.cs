using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services.Trading;
using Moq;
using Xunit;

namespace coretests.Stocks.Services
{
    public class TradingStrategyRunnerTests
    {
        private TradingStrategyRunner _runner;

        public TradingStrategyRunnerTests()
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
            mock.Setup(
                x => 
                    x.GetPriceHistory(
                        It.IsAny<UserState>(),
                        It.IsAny<string>(),
                        It.IsAny<PriceFrequency>(),
                        It.IsAny<System.DateTimeOffset>(),
                        It.IsAny<System.DateTimeOffset>()
                    )
                )
                .ReturnsAsync(
                    new ServiceResponse<PriceBar[]>(prices.ToArray())
                );

            _runner = new TradingStrategyRunner(mock.Object);
        }

        [Fact]
        public async Task BasicTest()
        {
            var results = await _runner.RunAsync(
                new UserState(),
                numberOfShares: 100,
                price: 10,
                stopPrice: 5,
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            var oneThirdResult = results.Results[0];

            var maxDrawdown = oneThirdResult.maxDrawdownPct;
            var maxGain = oneThirdResult.maxGainPct;
            var position = oneThirdResult.position;

            Assert.True(position.IsClosed);
            Assert.Equal(1005, position.Profit);
            Assert.Equal(100.50m, position.GainPct);
            Assert.Equal(2.01m, position.RR);
            Assert.Equal(0.0m, maxDrawdown);
            Assert.Equal(150.0m, maxGain);
            Assert.Equal(14, position.DaysHeld);

            var oneFourthResult = results.Results[1];
            maxDrawdown = oneFourthResult.maxDrawdownPct;
            maxGain = oneFourthResult.maxGainPct;
            position = oneFourthResult.position;

            Assert.True(position.IsClosed);
            Assert.Equal(1250m, position.Profit);
            Assert.Equal(125.0m, position.GainPct);
            Assert.Equal(2.5m, position.RR);
            Assert.Equal(0.0m, maxDrawdown);
            Assert.Equal(200.0m, maxGain);
            Assert.Equal(19, position.DaysHeld);
        }

        [Fact]
        public async Task WithPortionSizeTooSmall_StillSellsAtRRLevels()
        {
            var results = await _runner.RunAsync(
                new UserState(),
                numberOfShares: 2,
                price: 10,
                stopPrice: 5,
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            var result = results.Results[0];

            var maxDrawdown = result.maxDrawdownPct;
            var maxGain = result.maxGainPct;
            var position = result.position;

            Assert.True(position.IsClosed);
            Assert.Equal(15, position.Profit);
            Assert.Equal(75.0m, position.GainPct);
            Assert.Equal(1.5m, position.RR);
            Assert.Equal(0.0m, maxDrawdown);
            Assert.Equal(100m, maxGain);
            Assert.Equal(9, position.DaysHeld);
        }

        [Fact]
        public async Task WithPositionNotFullySold_IsOpen()
        {
            var result = await _runner.RunAsync(
                new UserState(),
                numberOfShares: 2,
                price: 50,
                stopPrice: 0.01m,
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            foreach(var r in result.Results)
            {
                Assert.False(r.position.IsClosed);
            }
        }
    }
}