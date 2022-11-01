#nullable enable
using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public class GapAnalysis
    {
        public static List<Gap> Generate(Span<PriceBar> prices, int numberOfBarsToAnalyze)
        {
            var start = prices.Length > numberOfBarsToAnalyze
                ? prices.Length - numberOfBarsToAnalyze
                : 0;
            return Generate(prices.Slice(start));
        }

        public static List<Gap> Generate(Span<PriceBar> prices)
        {
            var gaps = new List<Gap>();

            for (var i = 1; i < prices.Length; i++)
            {
                var yesterday = prices[i - 1];
                var currentBar = prices[i];

                var gapSizePct = 0m;

                if (currentBar.Low > yesterday.High)
                {
                    gapSizePct = Math.Round( (currentBar.Low - yesterday.High)/yesterday.High * 100, 2);
                }
                else if (currentBar.High < yesterday.Low)
                {
                    gapSizePct = -1 * Math.Round( (yesterday.Low - currentBar.High)/yesterday.Low * 100, 2);
                }

                if (gapSizePct != 0)
                {
                    var type = gapSizePct switch {
                        > 0 => GapType.Up,
                        < 0 => GapType.Down,
                        _ => throw new Exception("Invalid gap type")
                    };
                    var percentChange = Math.Round( (currentBar.Close - yesterday.Close)/yesterday.Close * 100, 2);
                    var gap = new Gap(
                        type: type,
                        gapSizePct: gapSizePct,
                        percentChange: percentChange,
                        bar: currentBar
                    );
                    gaps.Add(gap);
                }
            }

            return gaps;
        }
    }

    public record struct Gap(
        GapType type,
        decimal gapSizePct,
        decimal percentChange,
        PriceBar bar);

    public enum GapType { Up, Down }
}
#nullable restore