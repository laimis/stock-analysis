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
        public bool EarningsWarning
        {
            get
            {
                if (!DateTimeOffset.TryParse(this.Stats.NextEarningsDate, out var result))
                {
                    return false;
                }
                
                return result.Subtract(DateTimeOffset.UtcNow).TotalDays <= 30;
            }
        }

        public double? EarningsDaysLeft
        {
            get
            {
                if (!DateTimeOffset.TryParse(this.Stats.NextEarningsDate, out var result))
                {
                    return null;
                }
                
                return Math.Ceiling(result.Subtract(DateTimeOffset.UtcNow).TotalDays);
            }
        } 
    }
}