#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class GapAnalysis
    {
        public static List<Gap> Generate(Span<PriceBar> prices, int numberOfBarsToAnalyze)
        {
            var start = prices.Length > numberOfBarsToAnalyze
                ? prices.Length - numberOfBarsToAnalyze
                : 0;

            var volumeStart = prices.Length > numberOfBarsToAnalyze * 2
                ? prices.Length - numberOfBarsToAnalyze * 2
                : 0;

            var volumeStats = NumberAnalysis.Statistics(
                prices
                    .Slice(volumeStart, Math.Min(numberOfBarsToAnalyze, prices.Length))
                    .Select(p => p.Volume)
            );

            return Generate(prices.Slice(start), volumeStats);
        }

        private static List<Gap> Generate(Span<PriceBar> prices, DistributionStatistics volumeStats)
        {
            var gaps = new List<Gap>();

            for (var i = 1; i < prices.Length; i++)
            {
                var yesterday = prices[i - 1];
                var currentBar = prices[i];

                var gapSizePct = 0m;

                if (currentBar.Low > yesterday.High || currentBar.High < yesterday.Low)
                {
                    gapSizePct = (currentBar.Open - yesterday.Close)/yesterday.Close;
                }

                if (gapSizePct != 0)
                {
                    var type = gapSizePct switch {
                        > 0 => GapType.Up,
                        < 0 => GapType.Down,
                        _ => throw new Exception("Invalid gap type")
                    };
                    var percentChange = (currentBar.Close - yesterday.Close)/yesterday.Close;

                    Func<PriceBar, bool> closingCondition = gapSizePct switch {
                        > 0 => bar => bar.Close <= yesterday.Close,
                        < 0 => bar => bar.Close >= yesterday.Close,
                        _ => throw new Exception("Invalid gap type")
                    };

                    var closedQuickly = ClosingConditionMet(prices, i + 1, 10, closingCondition);
                    var open = !ClosingConditionMet(prices, i + 1, prices.Length - i, closingCondition);
                    var relativeVolume = Math.Round( currentBar.Volume / volumeStats.mean, 2);

                    var gap = new Gap(
                        type: type,
                        gapSizePct: gapSizePct,
                        percentChange: percentChange,
                        bar: currentBar,
                        closedQuickly: closedQuickly,
                        open: open,
                        relativeVolume: relativeVolume
                    );
                    gaps.Add(gap);
                }
            }

            return gaps;
        }

        private static bool ClosingConditionMet(
            Span<PriceBar> prices,
            int start, int length,
            Func<PriceBar, bool> closingCondition)
        {
            for(var i = start; i < start + length && i < prices.Length; i++)
            {
                if (closingCondition(prices[i]))
                {
                    return true;
                }
            }
            return false;
        }
    }

    public record struct Gap(
        GapType type,
        decimal gapSizePct,
        decimal percentChange,
        PriceBar bar,
        bool closedQuickly,
        bool open,
        decimal relativeVolume);

    public enum GapType { Up, Down }
}
#nullable restore