using System;
using core.Shared;
using core.Stocks;
using coretests.testdata;
using Xunit;

namespace coretests.Stocks
{
    public class PositionInstanceTests
    {
        private PositionInstance _position;

        public PositionInstanceTests()
        {
            _position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));

            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());
            _position.Sell(numberOfShares: 10, price: 40, when: DateTime.Parse("2020-02-25"), transactionId: Guid.NewGuid());
            _position.Sell(numberOfShares: 10, price: 37, when: DateTime.Parse("2020-03-21"), transactionId: Guid.NewGuid());
        }

        [Fact]
        public void LastSellPrice_Accurate() =>
            Assert.Equal(37, _position.LastSellPrice);

        [Fact]
        public void RR_Accurate() =>
            Assert.Equal(3.69m, _position.RR, 2);

        [Fact]
        public void GainPct_Accurate() =>
            Assert.Equal(0.185m, _position.GainPct, 2);

        [Fact]
        public void FirstBuyCost_Accurate() =>
            Assert.Equal(32.5m, _position.CompletedPositionCostPerShare);

        [Fact]
        public void FirstBuyNumberOfShares_Accurate() =>
            Assert.Equal(20, _position.CompletedPositionShares);

        [Fact]
        public void FirstStop_Accurate() =>
            Assert.Equal(30.875m, _position.FirstStop);

        [Fact]
        public void RiskedAmount_Accurate() =>
            Assert.Equal(32.5m, _position.RiskedAmount);

        [Fact]
        public void AverageCost_Accurate() =>
            Assert.Equal(32.5m, _position.AverageBuyCostPerShare);

        [Fact]
        public void DaysHeld() =>
            Assert.True(Math.Abs(57 - _position.DaysHeld) <= 1);

        [Fact]
        public void StopPriceGetsSetAfterSell() =>
            Assert.Equal(30.875m, _position.StopPrice);

        [Fact]
        public void PercentToStop_WithoutPrice_FullLoss() =>
            Assert.Null(_position.PercentToStop);

        [Fact]
        public void Cost()
        {
            var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());

            Assert.Equal(650, position.Cost);
        }

        [Fact]
        public void SetPrice_SetsVariousMetricsThatDependOnIt()
        {
            var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));

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
            Assert.Equal(0.2m, position.UnrealizedGainPct.Value, 2);
            Assert.Equal(0.2m, position.GainPct, 2);
            Assert.Equal(3.08m, position.UnrealizedRR.Value, 2);
            Assert.Equal(-0.43m, position.PercentToStop.Value, 2);
        }

        [Fact]
        public void PercentToStop_WithPriceButNoStop_SetToMax()
        {
            var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());

            position.SetPrice(40);

            Assert.Equal(-1, position.PercentToStop.Value);
        }

        [Fact]
        public void SetStop_SetsFirstStop()
        {
            var position = new PositionInstance(0, TestDataGenerator.TSLA, DateTime.Parse("2020-01-23"));

            position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            position.SetStopPrice(28, DateTimeOffset.UtcNow);
            position.SetStopPrice(29, DateTimeOffset.UtcNow);
            Assert.Equal(28, position.FirstStop);
        }

        [Fact]
        public void Profit() => Assert.Equal(120, _position.Profit);

        [Fact]
        public void IsClosed() => Assert.True(_position.IsClosed);

        [Fact]
        public void Ticker() => Assert.Equal(TestDataGenerator.TSLA, _position.Ticker);
    }
}