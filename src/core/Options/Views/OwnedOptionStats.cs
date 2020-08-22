using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStats
    {
        public OwnedOptionStats(IEnumerable<OwnedOptionSummary> summaries)
        {
            var transactions = summaries.ToList();
            if (transactions.Count == 0)
            {
                return;
            }

            this.Count = transactions.Count;
            this.Assigned = transactions.Count(s => s.Assigned);
            this.AveragePremiumCapture = transactions.Average(s => s.PremiumCapture);

            var wins = transactions.Where(s => s.Profit >= 0);
            if (wins.Any())
            {
                this.Wins = transactions.Count(s => s.Profit > 0);
                this.AvgWinAmount = wins.Average(s => s.Profit);
                this.MaxWinAmount = wins.Max(s => s.Profit);
            }

            var losses = transactions.Where(s => s.Profit < 0).ToList();
            if (losses.Count > 0)
            {
                this.Losses = losses.Count;
                this.AverageLossAmount = Math.Abs(losses.Average(s => s.Profit));
                this.MaxLossAmount = Math.Abs(losses.Min(s => s.Profit));
            }

            this.EV = (AvgWinAmount * Wins / Count) - (AverageLossAmount * Losses / Count);
            this.AverageProfitPerDay = transactions.Average(s => s.Profit / s.DaysHeld);
            this.AverageDays = transactions.Average(s => s.Days);
            this.AverageDaysHeld = transactions.Average(s => s.DaysHeld);
            this.AverageDaysHeldPercentage = AverageDaysHeld / AverageDays;
        }

        public int Count { get; }
        public int Wins { get; }
        public int Assigned { get; }
        public double AveragePremiumCapture { get; }

        public double AvgWinAmount { get; }
        public int Losses { get; }
        public double AverageLossAmount { get; }
        public double MaxWinAmount { get; }
        public double MaxLossAmount { get; }

        public double EV { get; }
        public double AverageProfitPerDay { get; }
        public double AverageDays { get; }
        public double AverageDaysHeld { get; }
        public double AverageDaysHeldPercentage { get; }
    }
}