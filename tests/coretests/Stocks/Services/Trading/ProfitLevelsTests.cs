using System;
using core.Stocks;
using core.Stocks.Services.Trading;
using Xunit;

namespace coretests.Stocks.Services.Trading
{
    public class ProfitLevelsTests
    {
        private PositionInstance _position;

        public ProfitLevelsTests()
        {
            _position = new PositionInstance(0, "TSLA");
            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());

            _position.SetStopPrice(27.5m, DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ProfitLevelsWork()
        {
            Assert.Equal(37.5m, ProfitPoints.GetProfitPointWithStopPrice(_position, 1));
            Assert.Equal(42.5m, ProfitPoints.GetProfitPointWithStopPrice(_position, 2));
            Assert.Equal(47.5m, ProfitPoints.GetProfitPointWithStopPrice(_position, 3));
            Assert.Equal(52.5m, ProfitPoints.GetProfitPointWithStopPrice(_position, 4));
            Assert.Equal(57.5m, ProfitPoints.GetProfitPointWithStopPrice(_position, 5));
        }

        [Fact]
        public void PercentLevelsWork()
        {
            Assert.Equal(34.125m, ProfitPoints.GetProfitPointWithPercentGain(_position, 1, 0.05m));
            Assert.Equal(35.75m, ProfitPoints.GetProfitPointWithPercentGain(_position, 2, 0.05m));
            Assert.Equal(37.375m, ProfitPoints.GetProfitPointWithPercentGain(_position, 3, 0.05m));
            Assert.Equal(39.0m, ProfitPoints.GetProfitPointWithPercentGain(_position, 4, 0.05m));
            Assert.Equal(40.625m, ProfitPoints.GetProfitPointWithPercentGain(_position, 5, 0.05m));
        }
    }
}