using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class PatternDetection
    {
        public static IEnumerable<Pattern> Generate(PriceBar[] bars)
        {
            var current = bars[^1];
            var previous = bars[^2];

            // upside reversal pattern detection
            if (current.Close > previous.Close && current.Low < previous.Low)
            {
                yield return new Pattern(date: current.Date, name: "Upside Reversal");
            }
        }
    }
}