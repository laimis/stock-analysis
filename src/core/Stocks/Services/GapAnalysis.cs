using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public class GapAnalysis
    {
        public static List<Gap> Generate(Span<PriceBar> prices)
        {
            var gaps = new List<Gap>();

            for (var i = 1; i < prices.Length; i++)
            {
                var yesterday = prices[i - 1];
                var currentBar = prices[i];

                var gap = 0m;

                if (currentBar.Low > yesterday.High)
                {
                    gap = Math.Round( (currentBar.Low - yesterday.High)/yesterday.High * 100, 2);
                }
                else if (currentBar.High < yesterday.Low)
                {
                    gap = -1 * Math.Round( (yesterday.Low - currentBar.High)/yesterday.Low * 100, 2);
                }

                if (gap != 0)
                {
                    var type = gap switch {
                        > 0 => GapType.Up,
                        < 0 => GapType.Down,
                        _ => throw new Exception("Invalid gap type")
                    };
                    gaps.Add(new Gap(type, gap, currentBar));
                }
            }

            return gaps;
        }
    }

    public record struct Gap(GapType type, decimal percentChange, PriceBar bar);
    public enum GapType { Up, Down }
}