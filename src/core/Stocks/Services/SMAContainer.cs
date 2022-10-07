#nullable enable
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
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

        public decimal? LastValue => Values?.Last();
    }

    public class SMAContainer
    {
        private SMA _sma20;
        private SMA _sma50;
        private SMA _sma150;
        private SMA _sma200;
        private SMA[] _all;

        public SMAContainer(SMA sma20, SMA sma50, SMA sma150, SMA sma200)
        {
            _sma20 = sma20;
            _sma50 = sma50;
            _sma150 = sma150;
            _sma200 = sma200;

            _all = new SMA[] { sma20, sma50, sma150, sma200 };
        }

        // public IReadOnlyList<SMA> All => _all;

        public int Length => _all.Length;

        public SMA sma20 => _sma20;
        public SMA sma50 => _sma50;
        public SMA sma150 => _sma150;
        public SMA sma200 => _sma200;


        public static SMAContainer Generate(HistoricalPrice[] prices)
        {
            return new SMAContainer(
                ToSMA(prices, 20),
                ToSMA(prices, 50),
                ToSMA(prices, 150),
                ToSMA(prices, 200)
            );
        }

        private static SMA ToSMA(HistoricalPrice[] prices, int interval)
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

        internal IEnumerable<SMA> GetEnumerable() => _all;

        internal decimal? LastValueOfSMA(int index) => _all[index].LastValue;
    }
}
#nullable restore