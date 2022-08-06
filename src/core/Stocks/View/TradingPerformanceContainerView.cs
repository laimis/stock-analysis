using System;
using System.Collections.Generic;

namespace core.Stocks.View
{
    public class TradingPerformanceContainerView
    {
        public TradingPerformanceContainerView() {}
        public TradingPerformanceContainerView(Span<PositionInstance> closedTransactions, int recentCount)
        {
            var recentStart = 0;
            var recentLengthToTake = closedTransactions.Length > recentCount ? recentCount : closedTransactions.Length;
            var recent = closedTransactions.Slice(recentStart, recentLengthToTake);

            Recent = TradingPerformanceView.Create(recent);
            Overall = TradingPerformanceView.Create(closedTransactions);

            // go over each closed transaction and calculate number of wins for 20 trades rolling window
            var wins = new List<int>();
            for(var i=0; i<closedTransactions.Length; i++)
            {
                var window = closedTransactions.Slice(i, Math.Min(20, closedTransactions.Length));
                var perfView = TradingPerformanceView.Create(window);
                wins.Add(perfView.Wins);

                if (i + 20 >= closedTransactions.Length)
                    break;
            }
            WinsRollingWindow = wins;
        }

        public TradingPerformanceView Recent { get; set; }
        public TradingPerformanceView Overall { get; set; }
        public List<int> WinsRollingWindow { get; }
    }
}