using System;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class PositionInstanceTests
    {
        private PositionInstance _position;

        public PositionInstanceTests()
        {
            _position = new PositionInstance("TSLA");

            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());
            _position.Sell(numberOfShares: 10, price: 40, when: DateTime.Parse("2020-02-25"), transactionId: Guid.NewGuid());
            _position.Sell(numberOfShares: 10, price: 37, when: DateTime.Parse("2020-03-21"), transactionId: Guid.NewGuid());
        }

        [Fact]
        public void RR_Accurate() =>
            Assert.Equal(1.5m, _position.RR, 2);

        [Fact]
        public void GainPct_Accurate() =>
            Assert.Equal(0.185m, _position.GainPct, 3);

        [Fact]
        public void RiskedAmount_Accurate() =>
            Assert.Equal(80, _position.RiskedAmount);

        [Fact]
        public void AverageCost_Accurate() =>
            Assert.Equal(32.5m, _position.AverageBuyCostPerShare);

        [Fact]
        public void DaysHeld() =>
            Assert.True(Math.Abs(57 - _position.DaysHeld) <= 1);

        [Fact]
        public void StopPriceGetsSetAfterSell() =>
            Assert.Equal(28.5m, _position.StopPrice);

        [Fact]
        public void PercentToStop_WithoutPrice_FullLoss() =>
            Assert.Equal(-1, _position.PercentToStop);

        [Fact]
        public void Cost()
        {
            var position = new PositionInstance("TSLA");

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());

            Assert.Equal(650, position.Cost);
        }

        [Fact]
        public void SetPrice_SetsVariousMetricsThatDependOnIt()
        {
            var position = new PositionInstance("TSLA");

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());
            position.Sell(numberOfShares: 5, price: 36, when: DateTime.Parse("2020-02-25"), transactionId: Guid.NewGuid());
            position.SetStopPrice(28, DateTimeOffset.UtcNow);

            Assert.Null(position.Price);
            Assert.Null(position.UnrealizedProfit);
            Assert.Null(position.UnrealizedGainPct);
            Assert.Null(position.PercentToStop);
            Assert.Null(position.UnrealizedRR);

            position.SetPrice(40);

            Assert.Equal(40, position.Price);
            Assert.Equal(100, position.UnrealizedProfit);
            Assert.Equal(0.23m, position.UnrealizedGainPct.Value, 2);
            Assert.Equal(0.11m, position.GainPct, 2);
            Assert.Equal(1.25m, position.UnrealizedRR);
            Assert.Equal(-0.43m, position.PercentToStop.Value, 2);
        }

        [Fact]
        public void Profit() => Assert.Equal(120, _position.Profit);

        [Fact]
        public void IsClosed() => Assert.True(_position.IsClosed);

        [Fact]
        public void Ticker() => Assert.Equal("TSLA", _position.Ticker);

        [Fact]
        public void RRLevels()
        {
            var position = new PositionInstance("TSLA");

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());

            Assert.Equal(4, position.RRLevels.Count);
            Assert.Equal(34.125m, position.RRLevels[0]);
            Assert.Equal(35.75m, position.RRLevels[1]);
            Assert.Equal(37.375m, position.RRLevels[2]);
            Assert.Equal(39m, position.RRLevels[3]);
        }
    }
}