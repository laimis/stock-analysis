using core.Adapters.Stocks;

namespace core.Stocks.View
{
    public class StockDetailView
    {
        public string Ticker { get; internal set; }
        public double Price { get; internal set; }
        public CompanyProfile Profile { get; internal set; }
        public StockAdvancedStats Stats { get; internal set; }
    }
}