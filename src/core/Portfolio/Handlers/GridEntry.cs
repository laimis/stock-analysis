using core.Adapters.Stocks;

namespace core.Portfolio
{
    public struct GridEntry
    {
        public GridEntry(string ticker, decimal price, StockAdvancedStats stats)
        {
            Price = price;
            Ticker = ticker;
            Stats = stats;
        }

        public string Ticker { get; }
        public StockAdvancedStats Stats { get; }
        public decimal Price { get; }

        public decimal Above50 => CompareToPrice(Stats.Day50MovingAvg);
        public decimal Above200 => CompareToPrice(Stats.Day200MovingAvg);

        private decimal CompareToPrice(decimal? value) =>
            value.HasValue switch {
                true => (Price - value.Value) / value.Value,
                false => 0
            };
    }
}