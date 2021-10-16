using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Stocks;

namespace core.Admin
{
    public struct EmailReviewEntryGroup
    {
        public EmailReviewEntryGroup(IEnumerable<EmailReviewEntry> entries, Price price, StockAdvancedStats stats)
        {
            Alerts = new List<EmailReviewEntry>();
            Ownership = new List<EmailReviewEntry>();
            Price = price;
            Ticker = null;
            Stats = stats;
            
            foreach(var e in entries.OrderByDescending(e => e.Created))
            {
                Ticker = e.Ticker;

                if (e.IsAlert)
                {
                    Alerts.Add(e);
                }
                else
                {
                    Ownership.Add(e);
                }
            }
        }

        public string Ticker { get; }
        public StockAdvancedStats Stats { get; }
        public List<EmailReviewEntry> Alerts { get; }
        public List<EmailReviewEntry> Ownership { get; }
        public Price Price { get; }

        public DateTimeOffset? EarningsDate
        {
            get
            {
                if (!DateTimeOffset.TryParse(Stats.NextEarningsDate, out var result))
                {
                    return null;
                }
                return result;
            }
        }

        public bool EarningsWarning 
            => 
                EarningsDate == null ? false 
                : EarningsDate.Value.Subtract(DateTimeOffset.UtcNow).TotalDays <= 30;

        public int EarningsDaysLeft
            => 
                EarningsDate == null ? -1
                : (int)Math.Ceiling(EarningsDate.Value.Subtract(DateTimeOffset.UtcNow).TotalDays);
    }
}