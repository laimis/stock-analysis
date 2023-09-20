using System;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using core.Stocks.Services.Analysis;

namespace core.Stocks.View
{
    public class PricesView
    {
        public PricesView(PriceBar[] prices)
        {
            Prices = prices;
            SMA = SMAContainer.Generate(prices);
            PercentChanges = NumberAnalysis.PercentChanges(prices);
        }

        public PriceBar[] Prices { get; }
        public SMAContainer SMA { get; }
        public DistributionStatistics PercentChanges { get; }
    }
}