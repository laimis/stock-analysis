using System;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class PositionInstanceWithExplicitStopPriceTests
    {
        private PositionInstance _position;

        public PositionInstanceWithExplicitStopPriceTests()
        {
            _position = new PositionInstance("TSLA");

            _position.Buy(numberOfShares: 10, price: 30, when: DateTime.Parse("2020-01-23"), transactionId: Guid.NewGuid());
            _position.Buy(numberOfShares: 10, price: 35, when: DateTime.Parse("2020-01-25"), transactionId: Guid.NewGuid());
            _position.SetStopPrice(20);
            _position.SetStopPrice(null); // subsequent assignment should be ignored because it's null
            _position.Sell(numberOfShares: 10, price: 40, when: DateTime.Parse("2020-02-25"), transactionId: Guid.NewGuid());
            _position.Sell(numberOfShares: 10, price: 37, when: DateTime.Parse("2020-03-21"), transactionId: Guid.NewGuid());
        }

        [Fact]
        public void RR_Accurate() => Assert.Equal(0.48m, _position.RR);

        [Fact]
        public void RiskedAmount_Accurate() => Assert.Equal(250m, _position.RiskedAmount.Value, 1);
    }
}