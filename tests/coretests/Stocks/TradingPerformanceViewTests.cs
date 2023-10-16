using System;
using core.fs.Services.Trading;
using Xunit;

namespace coretests.Stocks
{
    public class TradingPerformanceViewTests
    {
        private readonly TradingPerformance _performance =
            TradingPerformance.Create(
                TradingDataGenerator.GetClosedPositions()
                );

        [Fact]
        public void TestTotal() => Assert.Equal(3, _performance.NumberOfTrades);

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
        public void WinAvgReturnPctCorrect() => Assert.Equal(0.10m, _performance.WinAvgReturnPct);

        [Fact]
        public void WinMaxReturnPctCorrect() => Assert.Equal(0.10m, _performance.WinMaxReturnPct);

        [Fact]
        public void WinAvgDaysHeldCorrect() => Assert.Equal(1.0m, _performance.WinAvgDaysHeld);

        [Fact]
        public void LossAvgAmountCorrect() => Assert.Equal(-10m, _performance.AvgLossAmount);

        [Fact]
        public void LossMaxAmountCorrect() => Assert.Equal(-10m, _performance.MaxLossAmount);

        [Fact]
        public void LossAvgReturnPctCorrect() => Assert.Equal(-0.10m, _performance.LossAvgReturnPct);
        
        [Fact]
        public void LossMaxReturnPctCorrect() => Assert.Equal(-0.10m, _performance.LossMaxReturnPct);

        [Fact]
        public void LossAvgDaysHeldCorrect() => Assert.Equal(1.0m, _performance.LossAvgDaysHeld);

        [Fact]
        public void AvgDaysHeldCorrect() => Assert.Equal(1.0m, _performance.AverageDaysHeld);

        [Fact]
        public void EV_Correct() => Assert.Equal(16.67m, Math.Round(_performance.EV, 2));
    }
}