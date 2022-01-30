using System;

namespace core.Reports.Views
{
    public class SellView
    {
        public string Ticker { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal NumberOfShares { get; set; }
        public decimal Price { get; set; }
        public bool OlderThan30Days { get; set; }
        public int NumberOfDays => (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(Date).TotalDays);
        public decimal? CurrentPrice { get; set; }
        public decimal? Diff => CurrentPrice.HasValue ? (CurrentPrice.Value - Price) / Price : null;
    }
}