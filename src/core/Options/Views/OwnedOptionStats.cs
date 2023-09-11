using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Options
{
    public class OwnedOptionStatsView
    {
        public OwnedOptionStatsView(){}
        public OwnedOptionStatsView(IEnumerable<OwnedOptionView> open)
        {
            OpenOptions = open;
        }

        public IEnumerable<OwnedOptionView> OpenOptions { get; set; }
    }
    
    public class OwnedOptionStats
    {
        public OwnedOptionStats() {}
        public OwnedOptionStats(IEnumerable<OwnedOptionView> summaries)
        {
            var transactions = summaries.ToList();
            if (transactions.Count == 0)
            {
                return;
            }

            Count = transactions.Count;
            Assigned = transactions.Count(s => s.Assigned);
            AveragePremiumCapture = transactions.Average(s => s.PremiumCapture);

            var wins = transactions.Where(s => s.Profit >= 0);
            if (wins.Any())
            {
                Wins = transactions.Count(s => s.Profit > 0);
                AvgWinAmount = wins.Average(s => s.Profit);
                MaxWinAmount = wins.Max(s => s.Profit);
            }

            var losses = transactions.Where(s => s.Profit < 0).ToList();
            if (losses.Count > 0)
            {
                Losses = losses.Count;
                AverageLossAmount = Math.Abs(losses.Average(s => s.Profit));
                MaxLossAmount = Math.Abs(losses.Min(s => s.Profit));
            }

            EV = (AvgWinAmount * Wins / Count) - (AverageLossAmount * Losses / Count);
            AverageProfitPerDay = transactions.Average(s => s.Profit / s.DaysHeld);
            AverageDays = transactions.Average(s => s.Days);
            AverageDaysHeld = transactions.Average(s => s.DaysHeld);
            AverageDaysHeldPercentage = AverageDaysHeld / AverageDays;
        }

        public int Count { get; set; }
        public int Wins { get; set; }
        public int Assigned { get; set; }
        public decimal AveragePremiumCapture { get; set; }

        public decimal AvgWinAmount { get; set; }
        public int Losses { get; set; }
        public decimal AverageLossAmount { get; set; }
        public decimal MaxWinAmount { get; set; }
        public decimal MaxLossAmount { get; set; }

        public decimal EV { get; set; }
        public decimal AverageProfitPerDay { get; set; }
        public double AverageDays { get; set; }
        public double AverageDaysHeld { get; set; }
        public double AverageDaysHeldPercentage { get; set; }
    }
}