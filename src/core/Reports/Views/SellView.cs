using System;

namespace core.Reports.Views
{
    public class SellView
    {
        public string Ticker { get; set; }
        public DateTimeOffset Date { get; set; }
        public int NumberOfShares { get; set; }
        public double Price { get; set; }
        public bool OlderThan30Days { get; set; }
        public int NumberOfDays => (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(Date).TotalDays);
    }
}