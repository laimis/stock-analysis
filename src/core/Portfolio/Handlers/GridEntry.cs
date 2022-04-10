using core.Adapters.Stocks;

namespace core.Portfolio
{
    public struct GridEntry
    {
        public GridEntry(string ticker, Price price, StockAdvancedStats stats)
        {
            Price = price.Amount;
            Ticker = ticker;
            Stats = stats;
        }

        public string Ticker { get; }
        public StockAdvancedStats Stats { get; }
        public decimal Price { get; }
    }
}