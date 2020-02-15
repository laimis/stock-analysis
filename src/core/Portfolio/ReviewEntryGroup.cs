using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Stocks;

namespace core.Portfolio
{
    public struct ReviewEntryGroup
    {
        public ReviewEntryGroup(IEnumerable<ReviewEntry> entries, TickerPrice price, StockAdvancedStats stats)
        {
            this.Notes = new List<ReviewEntry>();
            this.Ownership = new List<ReviewEntry>();
            this.Price = price;
            this.Ticker = null;
            this.Stats = stats;
            
            foreach(var e in entries.OrderByDescending(e => e.Created))
            {
                this.Ticker = e.Ticker;

                if (e.IsNote)
                {
                    this.Notes.Add(e);
                }
                else
                {
                    this.Ownership.Add(e);
                }
            }
        }

        public string Ticker { get; }
        public StockAdvancedStats Stats { get; }
        public List<ReviewEntry> Notes { get; }
        public List<ReviewEntry> Ownership { get; }
        public TickerPrice Price { get; }

        public DateTimeOffset? EarningsDate
        {
            get
            {
                if (!DateTimeOffset.TryParse(this.Stats.NextEarningsDate, out var result))
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

        public double? EarningsDaysLeft
            => 
                EarningsDate == null ? (double?)null
                : Math.Ceiling(EarningsDate.Value.Subtract(DateTimeOffset.UtcNow).TotalDays);
    }
}