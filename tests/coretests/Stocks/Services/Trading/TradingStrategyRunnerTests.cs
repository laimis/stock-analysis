using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.fs.Services.TradingStrategies;
using core.fs.Shared;
using core.fs.Shared.Adapters.Brokerage;
using core.fs.Shared.Adapters.Stocks;
using core.Shared;
using core.Stocks;
using coretests.testdata;
using Moq;
using Xunit;

namespace coretests.Stocks.Services.Trading
{
    public class TradingStrategyRunnerTests
    {
        private static TradingStrategyRunner CreateRunner(
            int numberOfBars,
            Func<int, decimal> priceFunction)
        {
            var prices = GeneratePriceBars(numberOfBars, priceFunction);

            var mock = new Mock<IBrokerage>();
            mock.Setup(
                x => 
                    x.GetPriceHistory(
                        It.IsAny<UserState>(),
                        It.IsAny<Ticker>(),
                        It.IsAny<PriceFrequency>(),
                        It.IsAny<DateTimeOffset>(),
                        It.IsAny<DateTimeOffset>()
                    )
                )
                .ReturnsAsync(
                    new ServiceResponse<PriceBars>(prices)
                );

            return new TradingStrategyRunner(mock.Object, Mock.Of<IMarketHours>());
        }

        private static PriceBars GeneratePriceBars(int numberOfBars, Func<int, decimal> priceFunction)
        {
            var arr = Enumerable.Range(0, numberOfBars)
                .Select(i =>
                    new PriceBar(
                        date: DateTimeOffset.UtcNow.AddDays(i),
                        priceFunction(i), high: priceFunction(i) + 0.01m,
                        low: priceFunction(i) - 0.01m, close: priceFunction(i),
                        volume: 1000
                    )
                ).ToArray();
            
            return new PriceBars(arr);
        }

        [Fact]
        public async Task BasicTest()
        {
            var runner = CreateRunner(100, i => 10 + i);

            var results = await runner.Run(
                new UserState(),
                100,
                10,
                5,
                TestDataGenerator.TSLA,
                DateTimeOffset.UtcNow,
                false);

            var oneThirdResult = results.Results[0];

            var maxDrawdown = oneThirdResult.MaxDrawdownPct;
            var maxGain = oneThirdResult.MaxGainPct;
            var position = oneThirdResult.Position;

            Assert.True(position.IsClosed);
            Assert.Equal(1005, position.Profit);
            Assert.Equal(1.005m, position.GainPct);
            Assert.Equal(2.01m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(1.501m, maxGain);
            Assert.Equal(14, position.DaysHeld);

            var oneThirdPercentBased = results.Results[1];
            maxDrawdown = oneThirdPercentBased.MaxDrawdownPct;
            maxGain = oneThirdPercentBased.MaxGainPct;
            position = oneThirdPercentBased.Position;

            Assert.True(position.IsClosed);
            Assert.Equal(137.3m, position.Profit);
            Assert.Equal(0.1373m, position.GainPct);
            Assert.Equal(0.2746m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(0.201m, maxGain);
            Assert.Equal(1, position.DaysHeld);
        }

        [Fact]
        public void WithPortionSizeTooSmall_StillSellsAtRRLevels()
        {
            var bars = GeneratePriceBars(100, i => 10 + i);

            var testPosition = new PositionInstance(0, TestDataGenerator.TSLA, DateTimeOffset.UtcNow);
            testPosition.Buy(2, 10, DateTimeOffset.UtcNow, Guid.NewGuid());
            testPosition.SetStopPrice(5, DateTimeOffset.UtcNow);
            
            var runner = TradingStrategyFactory.createProfitPointsTrade(3);
            
            var result = runner.Run(testPosition, bars);
            
            var maxDrawdown = result.MaxDrawdownPct;
            var maxGain = result.MaxGainPct;
            var position = result.Position;
            
            Assert.True(position.IsClosed);
            Assert.Equal(15, position.Profit);
            Assert.Equal(0.75m, position.GainPct);
            Assert.Equal(1.5m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(1.001m, maxGain);
            Assert.Equal(9, position.DaysHeld);
        }

        [Fact]
        public async Task WithPositionNotFullySold_IsOpen()
        {
            var runner = CreateRunner(100, i => 10 + i);

            var result = await runner.Run(
                new UserState(),
                numberOfShares: 2,
                price: 50,
                stopPrice: 0.01m,
                ticker: TestDataGenerator.TSLA,
                DateTimeOffset.UtcNow,
                false);

            foreach(var r in result.Results.Take(1))
            {
                Assert.False(r.Position.IsClosed);
            }
        }

        [Fact]
        public void WithPriceFalling_StopPriceExitExecutes()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.createProfitPointsTrade(3).Run(
                positionInstance,
                bars
            );
            
            var position = result.Position;
            var maxGain = result.MaxGainPct;
            var maxDrawdown = result.MaxDrawdownPct;
            
            Assert.True(position.IsClosed);
            Assert.Equal(-25, position.Profit);
            Assert.Equal(-0.1m, position.GainPct);
            Assert.Equal(-1m, position.RR);
            Assert.Equal(4, position.DaysHeld);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
        }

        [Fact]
        public void CloseAfterFixedNumberOfDays_Works()
        {
            var data = GeneratePriceBars(10, i => 50 + i);

            var positionInstance = new PositionInstance(0, TestDataGenerator.TSLA, DateTimeOffset.UtcNow);
            positionInstance.Buy(
                numberOfShares: 5,
                price: 50,
                when: DateTimeOffset.UtcNow,
                Guid.NewGuid()
            );
            positionInstance.SetStopPrice(45, DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.createCloseAfterFixedNumberOfDays(5);
            
            var result = runner.Run(
                positionInstance,
                data
            );
            
            var position = result.Position;
            var maxGain = result.MaxGainPct;
            var maxDrawdown = result.MaxDrawdownPct;
            
            Assert.True(position.IsClosed);
            Assert.Equal(30, position.Profit);
            Assert.Equal(0.12m, position.GainPct);
            Assert.Equal(1.2m, position.RR);
            Assert.Equal(5, position.DaysHeld);
            Assert.Equal(0.1202m, maxGain);
            Assert.Equal(-0.0002m, maxDrawdown);
        }

        private (PriceBars bars, PositionInstance position) CreateDownsideTestData()
        {
            var bars = GeneratePriceBars(10, i => 50 - i);

            var positionInstance = new PositionInstance(0, TestDataGenerator.TSLA, DateTimeOffset.UtcNow);
            positionInstance.Buy(
                numberOfShares: 5,
                price: 50,
                when: DateTimeOffset.UtcNow,
                Guid.NewGuid()
            );

            positionInstance.SetStopPrice(
                45,
                DateTimeOffset.UtcNow
            );

            return (bars, positionInstance);
        }
    }
}