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

namespace coretests.Stocks.Services
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

            var results = await runner.RunAsync(
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

            var oneFourthResult = results.Results[1];
            maxDrawdown = oneFourthResult.maxDrawdownPct;
            maxGain = oneFourthResult.maxGainPct;
            position = oneFourthResult.position;

            Assert.True(position.IsClosed);
            Assert.Equal(1250m, position.Profit);
            Assert.Equal(1.250m, position.GainPct);
            Assert.Equal(2.5m, position.RR);
            Assert.Equal(-0.001m, maxDrawdown);
            Assert.Equal(2.001m, maxGain);
            Assert.Equal(19, position.DaysHeld);
            Assert.Equal(30, position.Price);
        }

        [Fact]
        public void WithPortionSizeTooSmall_StillSellsAtRRLevels()
        {
            var bars = GeneratePriceBars(100, i => 10 + i);

            var testPosition = new PositionInstance(0, "tsla");
            testPosition.Buy(2, 10, System.DateTimeOffset.UtcNow, Guid.NewGuid());
            testPosition.SetStopPrice(5, System.DateTimeOffset.UtcNow);

            var runner = TradingStrategyFactory.CreateProfitTakingStrategy("strat");

            var result = runner.Run(testPosition, bars.ToArray());

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

            var result = await runner.RunAsync(
                new UserState(),
                numberOfShares: 2,
                price: 50,
                stopPrice: 0.01m,
                ticker: "tsla",
                when: System.DateTimeOffset.UtcNow);

            foreach(var r in result.Results.Take(2))
            {
                Assert.False(r.position.IsClosed);
            }
        }

        [Fact]
        public void WithPriceFalling_StopPriceExitExecutes()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.CreateProfitTakingStrategy("strat").Run(
                positionInstance,
                bars.ToArray()
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
                    bars.ToArray()
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
                    bars.ToArray()
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
        public void WithPriceFallingAndLowAsStop_StopPriceExitExecutes()
        {
            var (bars, positionInstance) = CreateDownsideTestData();
            
            var result = TradingStrategyFactory.CreateProfitTakingStrategy("strat", useLowAsStop: true).Run(
                positionInstance,
                bars.ToArray()
            );

            var position = result.position;
            var maxGain = result.maxGainPct;
            var maxDrawdown = result.maxDrawdownPct;

            Assert.True(position.IsClosed);
            Assert.Equal(-25m, position.Profit);
            Assert.Equal(-0.1002m, position.GainPct);
            Assert.Equal(-1.002m, position.RR);
            Assert.Equal(4, position.DaysHeld);
            Assert.Equal(45, position.Price);
            Assert.Equal(0.0002m, maxGain);
            Assert.Equal(-0.1002m, maxDrawdown);
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