using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public class PercentChangeAnalysis
    {
        public static PercentChangeDescriptor Generate(Span<PriceBar> prices)
        {
            var closes = prices.ToArray().Select(x => x.Close);

            var percentChanges = closes
                .Zip(closes.Skip(1), (a, b) => (b - a) / a * 100)
                .ToArray();

                // calculate stats on the data
            var avg = Math.Round(percentChanges.Average(), 2);
            var stdDev = Math.Round((decimal)Math.Sqrt(percentChanges.Select(x => Math.Pow((double)(x - avg), 2)).Sum() / (percentChanges.Length - 1)), 2);
            var min = Math.Round(percentChanges.Min(), 2);
            var max = Math.Round(percentChanges.Max(), 2);
            var median = Math.Round(percentChanges.OrderBy(x => x).Skip(percentChanges.Length / 2).First(), 2);

            var buckets = PercentChangeFrequencies.Calculate(
                percentChanges,
                min,
                max
            );

            return new PercentChangeDescriptor(
                stdDev,
                min: min,
                max: max,
                mean: avg,
                median: median,
                buckets
            );
        }        
    }

    internal class PercentChangeFrequencies
    {
        internal static PercentChangeFrequency[] Calculate(
            decimal[] percentChanges,
            decimal min,
            decimal max)
        {
            var buckets = new Dictionary<int, int>();
            
            var boundaries = new int[] {
                -10, -9, -8, -7, -6, -5, -4, -3, -2, -1,
                0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10
            };
            
            foreach(var range in boundaries)
            {
                buckets.Add(range, 0);
            }

            foreach(var change in percentChanges)
            {
                var bucket = (int)Math.Floor(change);
                if (bucket < -10) bucket = -10;
                if (bucket > 10) bucket = 10;

                buckets[bucket]++;
            }

            return buckets
                .Select(kp => new PercentChangeFrequency(kp.Key, kp.Value))
                .OrderBy(f => f.percentChange)
                .ToArray();
        }
    }

    public record struct PercentChangeDescriptor(
        decimal standardDeviation,
        decimal min,
        decimal max,
        decimal mean,
        decimal median,
        PercentChangeFrequency[] buckets);
    public record struct PercentChangeFrequency(int percentChange, int frequency);
}