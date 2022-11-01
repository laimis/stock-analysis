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

                    Func<PriceBar, bool> closingCondition = gapSizePct switch {
                        > 0 => bar => bar.Close <= yesterday.Close,
                        < 0 => bar => bar.Close >= yesterday.Close,
                        _ => throw new Exception("Invalid gap type")
                    };

                    var closedQuickly = ClosingConditionMet(prices, i + 1, 10, closingCondition);
                    var open = !ClosingConditionMet(prices, i + 1, prices.Length - i, closingCondition);

                    var gap = new Gap(
                        type: type,
                        gapSizePct: gapSizePct,
                        percentChange: percentChange,
                        bar: currentBar,
                        closedQuickly: closedQuickly,
                        open: open
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
        bool open);

    public enum GapType { Up, Down }
}
#nullable restore