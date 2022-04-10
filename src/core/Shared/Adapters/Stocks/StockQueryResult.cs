namespace core.Adapters.Stocks
{
    public class StockQueryResult
    {
        public string Symbol { get; set; }
        public string CompanyName { get; set; }
        public double LatestPrice { get; set; }
        public string LatestSource { get; set; }
        public string LatestTime { get; set; }
        public long MarketCap { get; set; }
        public long Volume { get; set; }
        public double Week52High { get; set; }
        public double Week52Low { get; set; }
        public double? PERatio { get; set; }
    }
}