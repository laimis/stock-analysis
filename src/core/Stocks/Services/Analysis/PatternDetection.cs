using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class PatternDetection
    {
        public const string UpsideReversalName = "Upside Reversal";

        public static IEnumerable<Pattern> Generate(PriceBar[] bars)
        {
            if (bars.Length < 2)
            {
                yield break;
            }

            var current = bars[^1];
            var previous = bars[^2];

            var reversal = UpsideReversal(current, previous);
            if (reversal != null)
            {
                yield return reversal.Value;
            }
        }

        private static Pattern? UpsideReversal(PriceBar current, PriceBar previous)
        {
            // upside reversal pattern detection
            if (current.Close > previous.Close && current.Low < previous.Low)
            {
                return new Pattern(date: current.Date, name: UpsideReversalName);
            }

            return null;
        }   
    }
}