using System.Collections.Generic;

namespace core.Portfolio
{
    public struct ReviewEntryGroup
    {
        public ReviewEntryGroup(IEnumerable<ReviewEntry> entries, TickerPrice price)
        {
            this.Notes = new List<string>();
            this.Descriptions = new List<string>();
            this.Price = price;
            this.Ticker = null;
            this.ExpiresIn = null;

            foreach(var e in entries)
            {
                this.Ticker = e.Ticker;

                if (e.TTL != null)
                {
                    this.ExpiresIn = (int)e.TTL.Value.TotalDays;
                }

                if (e.IsNote)
                {
                    this.Notes.Add(e.Description);
                }
                else
                {
                    this.Descriptions.Add(e.Description);
                }
            }
        }

        public string Ticker { get; }
        public int? ExpiresIn { get; }
        public List<string> Notes { get; }
        public List<string> Descriptions { get; }
        public TickerPrice Price { get; }
    }
}