using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class PatternDetection
    {
        private static readonly System.Func<PriceBar[], Pattern?>[] _patternGenerators = new System.Func<PriceBar[], Pattern?>[]
        {
            UpsideReversal,
            Highest1YearVolume,
            XVolume
        };

        public static IEnumerable<Pattern> Generate(PriceBar[] bars)
        {
            foreach(var generator in _patternGenerators)
            {
                var pattern = generator(bars);
                if (pattern != null)
                {
                    yield return pattern.Value;
                }
            }
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
            if (current.Close > previous.Close && current.Low < previous.Low)
            {
                var strength = current.Close > System.Math.Max(previous.Open, previous.Close) ?
                    "Strong" : "Weak";

                return new Pattern(
                    date: current.Date,
                    name: UpsideReversalName,
                    description: $"{UpsideReversalName}: {strength}");
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
                return new Pattern(
                    date: bars[^1].Date,
                    name: Highest1YearVolumeName,
                    description: $"{Highest1YearVolumeName}: {bars[^1].Volume.ToString("N0")}");
            }

            return null;
        }

        public static string HighVolumeName => $"High Volume";
        private const int VolumeMultiplier = 5;
        private static Pattern? XVolume(PriceBar[] bars)
        {
            // if we get too little data, let's not infer that there is a pattern
            if (bars.Length < SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
            {
                return null;
            }

            // calculate the average volume of the last 60 bars
            var volumeSum = 0m;
            var numberOfBars = 0;
            for (var i = bars.Length - 1; i >= bars.Length - 60 && i >= 0; i--)
            {
                volumeSum += bars[i].Volume;
                numberOfBars++;
            }

            var averageVolume = volumeSum / numberOfBars;

            // now take the last bar volume
            var lastBarVolume = bars[^1].Volume;
            var multiplier = lastBarVolume / averageVolume;

            // if the last bar volume is 10x the average volume, then we have a pattern
            if (lastBarVolume > volumeSum * VolumeMultiplier)
            {
                return new Pattern(
                    date: bars[^1].Date,
                    name: HighVolumeName,
                    description: $"{HighVolumeName}: {bars[^1].Volume.ToString("N0")} (x{multiplier.ToString("N1")})");
            }

            return null;
        }
    }
}