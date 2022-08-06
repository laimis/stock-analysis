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
            Trends.Add(maxWin);
            Trends.Add(maxLoss);
        }

        public TradingPerformanceView Recent { get; set; }
        public TradingPerformanceView Overall { get; set; }
        public List<object> Trends { get; } = new List<object>();
    }
}