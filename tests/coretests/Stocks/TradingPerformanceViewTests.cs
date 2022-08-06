using System;
using System.Collections.Generic;
using core.Stocks;
using Xunit;

namespace coretests.Stocks
{
    public class TradingPerformanceViewTests
    {
        private TradingPerformanceView _performance;

        public TradingPerformanceViewTests()
        {
            // create a set of closed positions
            var closedPositions = new List<PositionInstance>();

            var position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 110m, DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 110m, DateTimeOffset.Now);
            closedPositions.Add(position);

            position = new PositionInstance("AMD");
            position.Buy(1, 100m, DateTimeOffset.Now.AddDays(-1));
            position.Sell(1, 90m, DateTimeOffset.Now);
            closedPositions.Add(position);

            _performance = new TradingPerformanceView(closedPositions);
        }
        [Fact]
        public void WinsCorrect() => Assert.Equal(2, _performance.Wins);

        [Fact]
        public void LossesCorrect() => Assert.Equal(1, _performance.Losses);

        [Fact]
        public void WinPctCorrect() => Assert.Equal(0.67m, Math.Round(_performance.WinPct, 2));

        [Fact]
        public void AvgWinAmountCorrect() => Assert.Equal(10m, _performance.AvgWinAmount);

        [Fact]
        public void MaxWinAmountCorrect() => Assert.Equal(10m, _performance.MaxWinAmount);

        [Fact]
        public void WinAvgReturnPctCorrect() => Assert.Equal(0.1m, _performance.WinAvgReturnPct);

        [Fact]
        public void WinMaxReturnPctCorrect() => Assert.Equal(0.1m, _performance.WinMaxReturnPct);

        [Fact]
        public void WinAvgDaysHeldCorrect() => Assert.Equal(1.0d, _performance.WinAvgDaysHeld);

        [Fact]
        public void LossAvgAmountCorrect() => Assert.Equal(10m, _performance.AvgLossAmount);

        [Fact]
        public void LossMaxAmountCorrect() => Assert.Equal(10m, _performance.MaxLossAmount);

        [Fact]
        public void LossAvgReturnPctCorrect() => Assert.Equal(0.1m, _performance.LossAvgReturnPct);
        
        [Fact]
        public void LossMaxReturnPctCorrect() => Assert.Equal(0.1m, _performance.LossMaxReturnPct);

        [Fact]
        public void LossAvgDaysHeldCorrect() => Assert.Equal(1.0d, _performance.LossAvgDaysHeld);

        [Fact]
        public void AvgDaysHeldCorrect() => Assert.Equal(1.0d, _performance.AvgDaysHeld);

        [Fact]
        public void EVCorrect() => Assert.Equal(3.33m, Math.Round(_performance.EV, 2));
    }
}