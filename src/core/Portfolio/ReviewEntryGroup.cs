using System;
using System.Collections.Generic;
using System.Linq;
using core.Adapters.Stocks;

namespace core.Portfolio
{
    public struct ReviewEntryGroup
    {
        public ReviewEntryGroup(IEnumerable<ReviewEntry> entries, TickerPrice price)
        {
            this.Notes = new List<NoteEntry>();
            this.Descriptions = new List<string>();
            this.Price = price;
            this.Ticker = null;
            this.ExpiresIn = null;

            foreach(var e in entries.OrderByDescending(e => e.Created))
            {
                this.Ticker = e.Ticker;

                if (e.TTL != null)
                {
                    this.ExpiresIn = (int)Math.Ceiling(Math.Abs(e.TTL.Value.TotalDays));
                }

                if (e.IsNote)
                {
                    this.Notes.Add(new NoteEntry(e.Created, e.Price, e.Stats, e.Description));
                }
                else
                {
                    this.Descriptions.Add(e.Description);
                }
            }
        }

        public string Ticker { get; }
        public int? ExpiresIn { get; }
        public List<NoteEntry> Notes { get; }
        public List<string> Descriptions { get; }
        public TickerPrice Price { get; }
    }

    public class NoteEntry
    {
        public NoteEntry(DateTimeOffset date, TickerPrice price, Adapters.Stocks.StockAdvancedStats stats, string text)
        {
            this.Date = date;
            this.Text = text;
            this.Price = price;
            this.Stats = stats;
        }

        public DateTimeOffset Date { get; }
        public string Text { get; }
        public TickerPrice Price { get; }
        public StockAdvancedStats Stats { get; }
    }
}