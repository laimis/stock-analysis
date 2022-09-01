namespace core.Shared.Adapters.Stocks
{
    public class HistoricalPrice
    {
        public string Date { get; set; }
        public decimal Close { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public decimal Volume { get; set; }
    }
}