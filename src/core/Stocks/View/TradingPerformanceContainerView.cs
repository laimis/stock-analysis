using System;
using System.Collections.Generic;
using core.Shared;

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
            var wins = new DataPointContainer<decimal>("Win %");
            var avgWinPct = new DataPointContainer<decimal>("Avg Win %");
            var avgLossPct = new DataPointContainer<decimal>("Avg Loss %");
            var ev = new DataPointContainer<decimal>("EV");
            var avgWinAmount = new DataPointContainer<decimal>("Avg Win $");
            var avgLossAmount = new DataPointContainer<decimal>("Avg Loss $");
            var rrPct = new DataPointContainer<decimal>("RR %");
            var rrAmount = new DataPointContainer<decimal>("RR $");
            var maxWin = new DataPointContainer<decimal>("Max Win $");
            var maxLoss = new DataPointContainer<decimal>("Max Loss $");
            var rrSum = new DataPointContainer<decimal>("RR Sum");

            closedTransactions.Reverse();

            for(var i=0; i<closedTransactions.Length; i++)
            {
                var window = closedTransactions.Slice(i, Math.Min(20, closedTransactions.Length));
                var perfView = TradingPerformanceView.Create(window);
                wins.Add(window[0].Closed.Value, perfView.WinPct);
                avgWinPct.Add(window[0].Closed.Value, perfView.WinAvgReturnPct);
                avgLossPct.Add(window[0].Closed.Value, perfView.LossAvgReturnPct);
                ev.Add(window[0].Closed.Value, perfView.EV);
                avgWinAmount.Add(window[0].Closed.Value, perfView.AvgWinAmount);
                avgLossAmount.Add(window[0].Closed.Value, perfView.AvgLossAmount);
                rrPct.Add(window[0].Closed.Value, perfView.WinAvgReturnPct / perfView.LossAvgReturnPct);
                rrAmount.Add(window[0].Closed.Value, perfView.AvgWinAmount / perfView.AvgLossAmount);
                maxWin.Add(window[0].Closed.Value, perfView.MaxWinAmount);
                maxLoss.Add(window[0].Closed.Value, perfView.MaxLossAmount);
                rrSum.Add(window[0].Closed.Value, perfView.rrSum);

                if (i + 20 >= closedTransactions.Length)
                    break;
            }

            Trends.Add(wins);
            Trends.Add(avgWinPct);
            Trends.Add(avgLossPct);
            Trends.Add(ev);
            Trends.Add(avgWinAmount);
            Trends.Add(avgLossAmount);
            Trends.Add(rrPct);
            Trends.Add(rrAmount);
            Trends.Add(rrSum);
            Trends.Add(maxWin);
            Trends.Add(maxLoss);

            // histogram of gains
            var gains = new DataPointContainer<decimal>("Gains");

            var min = Overall.MaxLossAmount * -1 - 100;
            var max = Overall.MaxWinAmount + 100;

            var buckets = 100;
            var step = (max - min) / buckets;

            for (var i = 0; i < buckets; i++)
            {
                var lower = min + (step * i);
                var upper = min + (step * (i + 1));
                var count = 0;
                for (var j = 0; j < closedTransactions.Length; j++)
                {
                    if (closedTransactions[j].Profit >= lower && closedTransactions[j].Profit < upper)
                        count++;
                }
                gains.Add(lower.ToString(), count);
            }

            Trends.Add(gains);

            // unreversed
            closedTransactions.Reverse();
        }

        public TradingPerformanceView Recent { get; set; }
        public TradingPerformanceView Overall { get; set; }
        public List<object> Trends { get; } = new List<object>();
    }
}