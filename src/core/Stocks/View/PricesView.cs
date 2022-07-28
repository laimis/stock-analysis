using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.View
{
    public class PricesView
    {
        public PricesView(HistoricalPrice[] prices, int[] smaIntervals)
        {
            Prices = prices;
            SMA = smaIntervals.Select(interval => ToSMA(prices, interval)).ToArray();
        }

        public HistoricalPrice[] Prices { get; }
        public SMA[] SMA { get; }

        private SMA ToSMA(HistoricalPrice[] prices, int interval)
        {
            var sma = new decimal?[prices.Length];
            for(var i = 0; i<prices.Length; i++)
            {
                if (i < interval)
                {
                    sma[i] = null;
                    continue;
                }

                var sum = 0m;
                for (var j = i - 1; j >= i - interval; j--)
                {
                    sum += prices[j].Close;
                }
                sma[i] = sum / interval;
            }
            return new SMA(sma, interval);
        }
    }

    public class SMA
    {
        public SMA(decimal?[] values, int interval)
        {
            Interval = interval;
            Values = values;
        }
        
        public int Interval { get; }
        public decimal?[] Values { get; }
        public string Description => $"SMA {Interval}";
    }
}