using core.Shared.Adapters.Stocks;
using core.Stocks.Services;

namespace core.Stocks.View
{
    public class PricesView
    {
        public PricesView(HistoricalPrice[] prices)
        {
            Prices = prices;
            SMA = core.Stocks.Services.SMA.Generate(prices);
        }

        public HistoricalPrice[] Prices { get; }
        public SMA[] SMA { get; }
    }
}