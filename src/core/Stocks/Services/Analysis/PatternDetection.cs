using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class PatternDetection
    {
        private static readonly Dictionary<string, System.Func<PriceBar[], Pattern?>> _patternGenerators =
            new Dictionary<string, System.Func<PriceBar[], Pattern?>>
        {
            {UpsideReversalName, UpsideReversal},
            {Highest1YearVolumeName, Highest1YearVolume},
            {HighVolumeName, HighVolume},
            {GapUpName, GapUp}
        };

        public static IEnumerable<Pattern> Generate(PriceBar[] bars)
        {
            foreach(var generator in _patternGenerators)
            {
                var pattern = generator.Value(bars);
                if (pattern != null)
                {
                    yield return pattern.Value;
                }
            }
        }

        public const string GapUpName = "Gap Up";
        private static Pattern? GapUp(PriceBar[] bars)
        {
            if (bars.Length < 2)
            {
                return null;
            }

            var current = bars[^1];
            
            var gaps = GapAnalysis.Generate(bars, 2);
            if (gaps.Count == 0 || gaps[0].type != GapType.Up)
            {
                return null;
            }

            var gap = gaps[0];

            return new Pattern(
                date: current.Date,
                name: GapUpName,
                description: $"{GapUpName} {(gap.gapSizePct * 100).ToString("N2")}",
                value: gap.gapSizePct,
                valueFormat: Shared.ValueFormat.Percentage
            );
        }

        public const string UpsideReversalName = "Upside Reversal";
        private static Pattern? UpsideReversal(PriceBar[] bars)
        {
            if (bars.Length < 2)
            {
                return null;
            }

            var current = bars[^1];
            var previous = bars[^2];

            // upside reversal pattern detection
            if (current.Close > System.Math.Max(previous.Open, previous.Close) && current.Low < previous.Low)
            {
                var additionalInfo = "";
                // see if we can do volume numbers
                if (bars.Length >= SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                {
                    var stats = NumberAnalysis.Statistics(
                        bars[(bars.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)..]
                        .Select(b => (decimal)b.Volume)
                        .ToArray()
                    );

                    var multiplier = current.Volume / stats.median;
                    additionalInfo = $", volume x{multiplier.ToString("N1")}";
                }

                return new Pattern(
                    date: current.Date,
                    name: UpsideReversalName,
                    description: $"{UpsideReversalName}{additionalInfo}",
                    value: current.Close,
                    valueFormat: Shared.ValueFormat.Currency);
            }

            return null;
        }

        public const string Highest1YearVolumeName = "Highest 1 year volume";
        private static Pattern? Highest1YearVolume(PriceBar[] bars)
        {
            if (bars.Length == 0)
            {
                return null;
            }
            
            // find the starting bar, which is higher date than 1 year ago
            var startIndex = bars.Length - 1;
            var thresholdDate = System.DateTime.Now.AddYears(-1);
            for (var i = startIndex; i >= 0; i--)
            {
                if (bars[i].Date < thresholdDate)
                {
                    startIndex = i;
                    break;
                }
            }

            // if the starting index is the first bar, see if that date is too recent
            if (bars[startIndex].Date > thresholdDate)
            {
                return null;
            }

            // now examine all bars from the starting bar to the end
            // and see if the last one has the highest volume
            var highestVolume = 0m;
            var highestVolumeIndex = -1;
            for (var i = startIndex; i < bars.Length; i++)
            {
                if (bars[i].Volume > highestVolume)
                {
                    highestVolume = bars[i].Volume;
                    highestVolumeIndex = i;
                }
            }

            if (highestVolumeIndex == bars.Length - 1)
            {
                var bar = bars[^1];

                return new Pattern(
                    date: bar.Date,
                    name: Highest1YearVolumeName,
                    description: $"{Highest1YearVolumeName}: {bar.Volume.ToString("N0")}",
                    value: bar.Volume,
                    valueFormat: Shared.ValueFormat.Number
                );
            }

            return null;
        }

        public const string HighVolumeName = $"High Volume";
        private const int VolumeMultiplier = 5;

        public static IEnumerable<string> AvailablePatterns => _patternGenerators.Keys;

        private static Pattern? HighVolume(PriceBar[] bars)
        {
            // if we get too little data, let's not infer that there is a pattern
            if (bars.Length < SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
            {
                return null;
            }

            var stats = NumberAnalysis.Statistics(
                bars[(bars.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)..]
                .Select(b => (decimal)b.Volume)
                .ToArray()
            );

            // now take the last bar volume
            var lastBarVolume = bars[^1].Volume;
            var multiplier = lastBarVolume / stats.median;

            // if the last bar volume is 10x the average volume, then we have a pattern
            if (lastBarVolume > stats.median * VolumeMultiplier)
            {
                var bar = bars[^1];
                return new Pattern(
                    date: bar.Date,
                    name: HighVolumeName,
                    description: $"{HighVolumeName}: {bar.Volume.ToString("N0")} (x{multiplier.ToString("N1")})",
                    value: bar.Volume,
                    valueFormat: Shared.ValueFormat.Number
                );
            }

            return null;
        }
    }
}