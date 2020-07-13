using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStats
    {
        public OwnedOptionStats(IEnumerable<OwnedOptionSummary> summaries)
        {
            var list = summaries.ToList();

            if (list.Count == 0)
            {
                return;
            }

            this.Count = list.Count;
            this.WinningTrades = list.Count(s => s.Profit > 0);
            this.Assigned = list.Count(s => s.Assigned);
            this.AveragePremiumCapture = list.Average(s => s.PremiumCapture);

            var positiveProfit = list.Where(s => s.Profit >= 0);
            if (positiveProfit.Any())
            {
                this.AverageWinAmount = positiveProfit.Average(s => s.Profit);
                this.MaxWin = list.Max(s => s.Profit);
            }

            var negativeProfit = list.Where(s => s.Profit < 0);
            if (negativeProfit.Any())
            {
                this.AverageLossAmount = negativeProfit.Average(s => s.Profit);
                this.MaxLoss = list.Min(s => s.Profit);
            }

            this.EV = (AverageWinAmount * WinningTrades / Count) + (AverageLossAmount * (Count - WinningTrades) / Count);
            this.AverageProfitPerDay = list.Average(s => s.Profit / s.DaysHeld);
            this.AverageDays = list.Average(s => s.Days);
            this.AverageDaysHeld = list.Average(s => s.DaysHeld);
            this.AverageDaysHeldPercentage = AverageDaysHeld / AverageDays;
        }

        public int Count { get; }
        public int WinningTrades { get; }
        public int Assigned { get; }
        public double AveragePremiumCapture { get; }

        public double AverageWinAmount { get; }
        public double AverageLossAmount { get; }
        public double MaxWin { get; }
        public double MaxLoss { get; }

        public double EV { get; }
        public double AverageProfitPerDay { get; }
        public double AverageDays { get; }
        public double AverageDaysHeld { get; }
        public double AverageDaysHeldPercentage { get; }
    }
}