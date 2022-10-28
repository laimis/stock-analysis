using System;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;

namespace core.Stocks.View
{
    public class PricesView
    {
        public PricesView(PriceBar[] prices)
        {
            Prices = prices;
            SMA = core.Stocks.Services.SMAContainer.Generate(prices);
        }

        public PriceBar[] Prices { get; }
        public SMAContainer SMA { get; }
    }
}