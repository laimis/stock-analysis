using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared;
using core.Stocks;
using core.Stocks.Services.Trading;

namespace core.Portfolio.Views
{
    public class TradingPerformanceContainerView
    {
        public TradingPerformanceContainerView() {}
        public TradingPerformanceContainerView(Span<PositionInstance> closedTransactions, int recentCount)
        {
            var recentStart = 0;
            var recentLengthToTake = closedTransactions.Length > recentCount ? recentCount : closedTransactions.Length;
            var recentClosedTransactions = closedTransactions.Slice(recentStart, recentLengthToTake);

            Recent = TradingPerformance.Create(recentClosedTransactions);
            Overall = TradingPerformance.Create(closedTransactions);

            // HACK: the input is with the most recent trade first, and we want to window from the start
            // so we reverse it here and then reverse it back on exit
            closedTransactions.Reverse();

            TrendsAll = GenerateTrends(closedTransactions, windowSize: recentCount);
            
            TrendsTwoMonths = GenerateTrends(
                TimeBasedSlice(closedTransactions, DateTime.Now.AddMonths(-2)),
                windowSize: recentCount);

            TrendsYTD = GenerateTrends(
                TimeBasedSlice(closedTransactions, new DateTime(DateTime.Now.Year, 1, 1)),
                windowSize: recentCount);

            TrendsOneYear = GenerateTrends(
                TimeBasedSlice(closedTransactions, DateTime.Now.AddYears(-1)),
                windowSize: recentCount);

            // unreversed
            closedTransactions.Reverse();
        }

        private static Span<PositionInstance> TimeBasedSlice(
            Span<PositionInstance> closedTransactions,
            DateTime dateThreshold)
        {
            var startIndex = 0;
            for (var i = closedTransactions.Length - 1; i >= 0; i--)
            {
                if (closedTransactions[i].Closed.Value < dateThreshold)
                {
                    startIndex = i;
                    break;
                }
            }
            return closedTransactions.Slice(startIndex, closedTransactions.Length - startIndex);
        }

        private List<object> GenerateTrends(Span<PositionInstance> positions, int windowSize)
        {
            var trends = new List<object>();

            // go over each closed transaction and calculate number of wins for 20 trades rolling window
            var profits = new DataPointContainer<decimal>("Profits", DataPointChartType.line);
            var wins = new DataPointContainer<decimal>("Win %", DataPointChartType.line);
            var avgWinPct = new DataPointContainer<decimal>("Avg Win %", DataPointChartType.line);
            var avgLossPct = new DataPointContainer<decimal>("Avg Loss %", DataPointChartType.line);
            var ev = new DataPointContainer<decimal>("EV", DataPointChartType.line);
            var avgWinAmount = new DataPointContainer<decimal>("Avg Win $", DataPointChartType.line);
            var avgLossAmount = new DataPointContainer<decimal>("Avg Loss $", DataPointChartType.line);
            var rrPct = new DataPointContainer<decimal>("RR %", DataPointChartType.line);
            var rrAmount = new DataPointContainer<decimal>("RR $", DataPointChartType.line);
            var maxWin = new DataPointContainer<decimal>("Max Win $", DataPointChartType.line);
            var maxLoss = new DataPointContainer<decimal>("Max Loss $", DataPointChartType.line);
            var rrSum = new DataPointContainer<decimal>("RR Sum", DataPointChartType.line);

            for (var i = 0; i < positions.Length; i++)
            {
                var window = positions.Slice(i, Math.Min(windowSize, positions.Length));
                var perfView = TradingPerformance.Create(window);
                profits.Add(window[0].Closed.Value, perfView.Profit);
                wins.Add(window[0].Closed.Value, perfView.WinPct);
                avgWinPct.Add(window[0].Closed.Value, perfView.WinAvgReturnPct);
                avgLossPct.Add(window[0].Closed.Value, perfView.LossAvgReturnPct);
                ev.Add(window[0].Closed.Value, perfView.EV);
                avgWinAmount.Add(window[0].Closed.Value, perfView.AvgWinAmount);
                avgLossAmount.Add(window[0].Closed.Value, perfView.AvgLossAmount);
                rrPct.Add(window[0].Closed.Value, perfView.ReturnPctRatio);
                rrAmount.Add(window[0].Closed.Value, perfView.ProfitRatio);
                maxWin.Add(window[0].Closed.Value, perfView.MaxWinAmount);
                maxLoss.Add(window[0].Closed.Value, perfView.MaxLossAmount);
                rrSum.Add(window[0].Closed.Value, perfView.rrSum);
                
                if (i + 20 >= positions.Length)
                    break;
            }

            // non windowed stats
            var agrades = 0;
            var bgrades = 0;
            var cgrades = 0;
            var equityCurve = new DataPointContainer<decimal>("Equity Curve", DataPointChartType.line);
            var equity = 0m;
            var minCloseDate = DateTimeOffset.MaxValue;
            var maxCloseDate = DateTimeOffset.MinValue;
            var positionsClosedByDate = new Dictionary<DateTimeOffset, int>();
            var positionsClosedByDateContainer = new DataPointContainer<decimal>("Positions Closed", DataPointChartType.bar);
            var positionsOpenedByDate = new Dictionary<DateTimeOffset, int>();
            var positionsOpenedByDateContainer = new DataPointContainer<decimal>("Positions Opened", DataPointChartType.bar);
            
            for (var i = 0; i < positions.Length; i++)
            {
                equity += positions[i].Profit;
                equityCurve.Add(positions[i].Closed.Value, equity);
                if (positions[i].Grade == "A")
                    agrades++;
                if (positions[i].Grade == "B")
                    bgrades++;
                if (positions[i].Grade == "C")
                    cgrades++;

                if (positions[i].Closed.Value < minCloseDate)
                    minCloseDate = positions[i].Closed.Value;
                if (positions[i].Closed.Value > maxCloseDate)
                    maxCloseDate = positions[i].Closed.Value;

                if (positionsClosedByDate.ContainsKey(positions[i].Closed.Value.Date))
                    positionsClosedByDate[positions[i].Closed.Value.Date]++;
                else
                    positionsClosedByDate[positions[i].Closed.Value.Date] = 1;

                if (positionsOpenedByDate.ContainsKey(positions[i].Opened.Value.Date))
                    positionsOpenedByDate[positions[i].Opened.Value.Date]++;
                else
                    positionsOpenedByDate[positions[i].Opened.Value.Date] = 1;
            }

            // for the range that the positions are in, calculate the number of buys for each date
            for(var d = minCloseDate; d < maxCloseDate; d = d.AddDays(1))
            {
                positionsClosedByDateContainer.Add(d, positionsClosedByDate.ContainsKey(d.Date) ? positionsClosedByDate[d.Date] : 0);
                positionsOpenedByDateContainer.Add(d, positionsOpenedByDate.ContainsKey(d.Date) ? positionsOpenedByDate[d.Date] : 0);
            }

            var gradeContainer = new DataPointContainer<decimal>("Grade", DataPointChartType.bar);
            gradeContainer.Add("A", agrades);
            gradeContainer.Add("B", bgrades);
            gradeContainer.Add("C", cgrades);

            var gainDistribution = GenerateOutcomeHistogram(
                "Gain Distribution",
                positions,
                p => p.Profit,
                buckets: 20,
                symmetric: true);

            var rrDistribution = GenerateOutcomeHistogram(
                "RR Distribution",
                positions,
                p => p.RR,
                buckets: 10,
                symmetric: true);

            trends.Add(profits);
            trends.Add(equityCurve);
            trends.Add(gradeContainer);
            trends.Add(gainDistribution);
            trends.Add(rrDistribution);
            trends.Add(wins);
            trends.Add(avgWinPct);
            trends.Add(avgLossPct);
            trends.Add(ev);
            trends.Add(avgWinAmount);
            trends.Add(avgLossAmount);
            trends.Add(rrPct);
            trends.Add(rrAmount);
            trends.Add(rrSum);
            trends.Add(maxWin);
            trends.Add(maxLoss);
            trends.Add(positionsOpenedByDateContainer);
            trends.Add(positionsClosedByDateContainer);

            return trends;
        }

        private static DataPointContainer<decimal> GenerateOutcomeHistogram(
            string histogramLabel,
            Span<PositionInstance> transactionsToUse,
            Func<PositionInstance, decimal> valueFunc,
            int buckets = 50,
            bool symmetric = false)
        {
            var gains = new DataPointContainer<decimal>(histogramLabel, DataPointChartType.bar);

            var min = Decimal.MaxValue;
            var max = Decimal.MinValue;

            // first, disover min and max
            foreach(var transaction in transactionsToUse)
            {
                var value = valueFunc(transaction);
                if (value < min)
                    min = value;
                if (value > max)
                    max = value;
            }

            min = Math.Floor(min);
            max = Math.Ceiling(max);
            
            if (symmetric)
            {
                var absMax = Math.Max(Math.Abs(min), Math.Abs(max));
                min = -absMax;
                max = absMax;
            }

            var step = (max - min) / buckets;
            step = step switch {
                var x when x < 1 => Math.Round(step, 4),
                _ => Math.Round(step, 0)
            };

            for (var i = 0; i <= buckets; i++)
            {
                var lower = min + (step * i);
                var upper = min + (step * (i + 1));
                var count = 0;
                for (var j = 0; j < transactionsToUse.Length; j++)
                {
                    var value = valueFunc(transactionsToUse[j]);
                    if (value >= lower && value < upper)
                        count++;
                }
                gains.Add(lower.ToString(), count);
            }

            return gains;
        }

        public TradingPerformance Recent { get; }
        public TradingPerformance Overall { get; }
        public List<object> TrendsAll { get; }
        public List<object> TrendsTwoMonths { get; }
        public List<object> TrendsYTD { get; }
        public List<object> TrendsOneYear { get; }
    }
}