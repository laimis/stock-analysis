using System;
using core.fs.Services.Trading;
using core.Stocks;
using Xunit;

namespace coretests.Stocks.Services.Trading
{
    public class ProfitPointsTests
    {
        private PositionInstance _position;

        public ProfitPointsTests()
        {
            _position = new PositionInstance(0, "TSLA", DateTime.Parse("2020-01-23"));
            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());

            _position.SetStopPrice(27.5m, DateTimeOffset.UtcNow);
        }

        [Fact]
        public void ProfitLevelsWork()
        {
            Assert.Equal(37.5m, ProfitPoints.getProfitPointWithStopPrice(_position, 1));
            Assert.Equal(42.5m, ProfitPoints.getProfitPointWithStopPrice(_position, 2));
            Assert.Equal(47.5m, ProfitPoints.getProfitPointWithStopPrice(_position, 3));
            Assert.Equal(52.5m, ProfitPoints.getProfitPointWithStopPrice(_position, 4));
            Assert.Equal(57.5m, ProfitPoints.getProfitPointWithStopPrice(_position, 5));
        }

        [Fact]
        public void PercentLevelsWork()
        {
            Assert.Equal(34.125m, ProfitPoints.getProfitPointWithPercentGain(_position, 1, 0.05m));
            Assert.Equal(35.75m, ProfitPoints.getProfitPointWithPercentGain(_position, 2, 0.05m));
            Assert.Equal(37.375m, ProfitPoints.getProfitPointWithPercentGain(_position, 3, 0.05m));
            Assert.Equal(39.0m, ProfitPoints.getProfitPointWithPercentGain(_position, 4, 0.05m));
            Assert.Equal(40.625m, ProfitPoints.getProfitPointWithPercentGain(_position, 5, 0.05m));
        }
    }
}