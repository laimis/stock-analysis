using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks;
using core.Stocks.Services.Trading;
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
                        It.IsAny<string>(),
                        It.IsAny<PriceFrequency>(),
                        It.IsAny<System.DateTimeOffset>(),
                        It.IsAny<System.DateTimeOffset>()
                    )
                )
                .ReturnsAsync(
                    new ServiceResponse<PriceBar[]>(prices.ToArray())
                );

            return new TradingStrategyRunner(mock.Object, Mock.Of<IMarketHours>());
        }

        private static List<PriceBar> GeneratePriceBars(int numberOfBars, Func<int, decimal> priceFunction)
        {
            return Enumerable.Range(0, numberOfBars)
                .Select(i =>
                    new PriceBar(
                        date: System.DateTimeOffset.UtcNow.AddDays(i),
                        open: priceFunction(i), high: priceFunction(i) + 0.01m,
                        low: priceFunction(i) - 0.01m, close: priceFunction(i),
                        volume: 1000
                    )
                ).ToList();
        }

        [Fact]
        public async Task BasicTest()
        {
            var runner = CreateRunner(100, i => 10 + i);

            var results = await runner.Run(
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
            Assert.Equal(1.005m, position.GainPct);
            Assert.Equal(2.01m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(1.501m, maxGain);
            Assert.Equal(14, position.DaysHeld);
            Assert.Equal(25, position.Price);

            var oneThirdPercentBased = results.Results[1];
            maxDrawdown = oneThirdPercentBased.maxDrawdownPct;
            maxGain = oneThirdPercentBased.maxGainPct;
            position = oneThirdPercentBased.position;

            Assert.True(position.IsClosed);
            Assert.Equal(140.7m, position.Profit);
            Assert.Equal(0.1407m, position.GainPct);
            Assert.Equal(0.2814m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(0.301m, maxGain);
            Assert.Equal(2, position.DaysHeld);
            Assert.Equal(13, position.Price);
        }

        [Fact]
        public void WithPortionSizeTooSmall_StillSellsAtRRLevels()
        {
            var bars = GeneratePriceBars(100, i => 10 + i);

            var testPosition = new PositionInstance(0, "tsla");
            testPosition.Buy(2, 10, System.DateTimeOffset.UtcNow, Guid.NewGuid());
            testPosition.SetStopPrice(5, System.DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.CreateProfitTakingStrategy();

            var result = runner.Run(testPosition, bars);

            var maxDrawdown = result.maxDrawdownPct;
            var maxGain = result.maxGainPct;
            var position = result.position;

            Assert.True(position.IsClosed);
            Assert.Equal(15, position.Profit);
            Assert.Equal(0.75m, position.GainPct);
            Assert.Equal(1.5m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(1.001m, maxGain);
            Assert.Equal(9, position.DaysHeld);
            Assert.Equal(20, position.Price);
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
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            foreach(var r in result.Results.Take(1))
            {
                Assert.False(r.position.IsClosed);
            }
        }

        [Fact]
        public void WithPriceFalling_StopPriceExitExecutes()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.CreateProfitTakingStrategy().Run(
                positionInstance,
                bars
            );

            var position = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(position.IsClosed);
            Assert.Equal(-25, position.Profit);
            Assert.Equal(-0.1m, position.GainPct);
            Assert.Equal(-1m, position.RR);
            Assert.Equal(4, position.DaysHeld);
            Assert.Equal(45, position.Price);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
        }

        [Fact]
        public void WithPriceFallingAndDownsideProtectionOn_LossesSmaller()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.CreateOneThirdRRWithDownsideProtection(
                downsideProtectionSize: 2)
                .Run(
                    positionInstance,
                    bars
                );

            var position = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(position.IsClosed);
            Assert.Equal(-21, position.Profit);
            Assert.Equal(-0.084m, position.GainPct, 3);
            Assert.Equal(-0.84m, position.RR);
            Assert.Equal(4, position.DaysHeld);
            Assert.Equal(45, position.Price);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
        }

        [Fact]
        public void WithPriceFallingAndDownsideProtectionOnSmallerSize_LossesSmaller()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.CreateOneThirdRRWithDownsideProtection(
                downsideProtectionSize: 3)
                .Run(
                    positionInstance,
                    bars
                );

            var position = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(position.IsClosed);
            Assert.Equal(-23, position.Profit);
            Assert.Equal(-0.092m, position.GainPct, 3);
            Assert.Equal(-0.92m, position.RR);
            Assert.Equal(4, position.DaysHeld);
            Assert.Equal(45, position.Price);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
        }

        [Fact]
        public void CloseAfterFixedNumberOfDays_Works()
        {
            var data = GeneratePriceBars(10, i => 50 + i);

            var positionInstance = new PositionInstance(0, "tsla");
            positionInstance.Buy(
                numberOfShares: 5,
                price: 50,
                when: DateTimeOffset.UtcNow,
                Guid.NewGuid()
            );
            positionInstance.SetStopPrice(45, DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.CreateCloseAfterFixedNumberOfDays(5);

            var result = runner.Run(
                positionInstance,
                data
            );

            var position = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(position.IsClosed);
            Assert.Equal(30, position.Profit);
            Assert.Equal(0.12m, position.GainPct);
            Assert.Equal(1.2m, position.RR);
            Assert.Equal(5, position.DaysHeld);
            Assert.Equal(56, position.Price);
            Assert.Equal(0.1202m, maxGain);
            Assert.Equal(-0.0002m, maxDrawdown);
        }

        [Fact]
        public void CloseAfterFixedNumberOfDaysWithStop_RespectsStop()
        {
            var bars = GeneratePriceBars(20, i => 50 - i);

            var position = new PositionInstance(0, "tsla");
            position.Buy(
                numberOfShares: 5,
                price: 50,
                when: DateTimeOffset.UtcNow,
                Guid.NewGuid()
            );
            position.SetStopPrice(45, DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.CreateCloseAfterFixedNumberOfDaysRespectStop(10);

            var result = runner.Run(
                position,
                bars
            );

            var positionInstance = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(positionInstance.IsClosed);
            Assert.Equal(-25, positionInstance.Profit);
            Assert.Equal(-0.1m, positionInstance.GainPct);
            Assert.Equal(-1m, positionInstance.RR);
            Assert.Equal(4, positionInstance.DaysHeld);
            Assert.Equal(45, positionInstance.Price);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
        }

        [Fact]
        public void WithAdvancingStops_Works()
        {
            var bars = GeneratePriceBars(50, i => 50 + i);

            var position = new PositionInstance(0, "tsla");
            position.Buy(
                numberOfShares: 5,
                price: 50,
                when: DateTimeOffset.UtcNow,
                Guid.NewGuid()
            );
            position.SetStopPrice(45, DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.CreateWithAdvancingStops();

            var result = runner.Run(
                position,
                bars
            );

            var positionInstance = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            // the price keeps on increasing, we are not selling as it does not
            // approach stop
            Assert.False(positionInstance.IsClosed);
            Assert.Equal(0, positionInstance.Profit); // no profit yet, no sells
            Assert.Equal(245, positionInstance.UnrealizedProfit);
            Assert.Equal(0.98m, positionInstance.GainPct);
            Assert.Equal(0.98m, positionInstance.UnrealizedGainPct);
            Assert.Equal(9.8m, positionInstance.RR);
            Assert.Equal(9.8m, positionInstance.UnrealizedRR);
            Assert.Equal(99, positionInstance.Price);
            Assert.Equal(0.9802m, maxGain);
            Assert.Equal(-0.0002m, maxDrawdown);
        }

        private (List<PriceBar> bars, PositionInstance position) CreateDownsideTestData()
        {
            var bars = GeneratePriceBars(10, i => 50 - i);

            var positionInstance = new PositionInstance(0, "tsla");
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