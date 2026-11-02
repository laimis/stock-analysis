using System;
using core.Shared;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class PositionInstanceTests
    {
        private static PositionInstance NewPosition() =>
            new(1, new Ticker("TEST"), DateTimeOffset.UtcNow);

        [Fact]
        public void Sell_FractionalSharesLeavingResidueBelowThreshold_ClosesPosition()
        {
            var position = NewPosition();
            var now = DateTimeOffset.UtcNow;

            position.Buy(2.1531m, 620.70m, now, Guid.NewGuid());
            position.Sell(2.1527m, 712.68m, Guid.NewGuid(), now);

            Assert.Equal(0, position.NumberOfShares);
            Assert.True(position.IsClosed);
        }

        [Fact]
        public void Sell_FractionalSharesLeavingResidueAboveThreshold_StaysOpen()
        {
            var position = NewPosition();
            var now = DateTimeOffset.UtcNow;

            position.Buy(2.1531m, 620.70m, now, Guid.NewGuid());
            position.Sell(2.1m, 712.68m, Guid.NewGuid(), now);

            Assert.Equal(0.0531m, position.NumberOfShares);
            Assert.False(position.IsClosed);
        }
    }
}
