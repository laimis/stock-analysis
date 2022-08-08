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

            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"));
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"));
            _position.Sell(amount: 10, price: 40, when: DateTime.Parse("2020-02-25"));
            _position.Sell(amount: 10, price: 37, when: DateTime.Parse("2020-03-21"));
        }

        [Fact]
        public void RR_Accurate() => Assert.Equal(3.69m, _position.RR, 2);

        [Fact]
        public void RiskedPct_Accurate() => Assert.Equal(0.05m, _position.RiskedPct);

        [Fact]
        public void RiskedAmount_Accurate() => Assert.Equal(32.5m, _position.RiskedAmount);

        [Fact]
        public void AverageCost_Accurate() => Assert.Equal(32.5m, _position.AveragePrice);

        [Fact]
        public void DaysHeld()
        {
            Assert.True(Math.Abs(57 - _position.DaysHeld) <= 1);
        }

        [Fact]
        public void Cost()
        {
            Assert.Equal(650, _position.Cost);
        }

        [Fact]
        public void Return()
        {
            Assert.Equal(770, _position.Return);
        }

        [Fact]
        public void Percentage()
        {
            Assert.Equal(0.1846m, _position.ReturnPct);
        }

        [Fact]
        public void Profit()
        {
            Assert.Equal(120, _position.Profit);
        }

        [Fact]
        public void IsClosed()
        {
            Assert.True(_position.IsClosed);
        }

        [Fact]
        public void Ticker()
        {
            Assert.Equal("TSLA", _position.Ticker);
        }
    }
}