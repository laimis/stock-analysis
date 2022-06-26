using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.View
{
    public class PricesView
    {
        public PricesView(HistoricalPrice[] prices, int[] ints)
        {
            Prices = prices;
            SMA = ints.Select(i => ToSMA(prices, i)).ToArray();
        }

        public HistoricalPrice[] Prices { get; }
        public SMA[] SMA { get; }

        private SMA ToSMA(HistoricalPrice[] success, int interval)
        {
            var sma = new decimal?[success.Length];
            for(var i = 0; i<success.Length-1; i++)
            {
                if (i < interval)
                {
                    sma[i] = null;
                    continue;
                }

                var sum = 0m;
                for (var j = i; j >= i - interval; j--)
                {
                    sum += success[j].Close;
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