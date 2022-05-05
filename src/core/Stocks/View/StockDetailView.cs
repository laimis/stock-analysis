using core.Adapters.Stocks;

namespace core.Stocks.View
{
    public class StockDetailsView
    {
        public string Ticker { get; internal set; }
        public decimal Price { get; internal set; }
        public CompanyProfile Profile { get; internal set; }
        public StockAdvancedStats Stats { get; internal set; }
    }
}