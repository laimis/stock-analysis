using System;
using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.Services
{
    public class NumberAnalysis
    {
        public static DistributionStatistics PercentChanges(Span<decimal> numbers)
        {
            var percentChanges = new decimal[numbers.Length - 1];
            for(var i = 1; i < numbers.Length; i++)
            {
                var percentChange = (numbers[i] - numbers[i-1])/numbers[i-1] * 100;
                percentChanges[i-1] = percentChange;
            }

            return Statistics(percentChanges);
        }

        public static DistributionStatistics Statistics(decimal[] numbers)
        {
           // calculate stats on the data
            var mean = Math.Round(numbers.Average(), 2);
            var stdDev = Math.Round((decimal)Math.Sqrt(numbers.Select(x => Math.Pow((double)(x - mean), 2)).Sum() / (numbers.Length - 1)), 2);
            var min = Math.Round(numbers.Min(), 2);
            var max = Math.Round(numbers.Max(), 2);
            var median = Math.Round(numbers.OrderBy(x => x).Skip(numbers.Length / 2).First(), 2);
            var count = numbers.Length;
            var skewness = Math.Round(
                (decimal)(numbers.Select(x => Math.Pow((double)(x - mean), 3)).Sum() / count / Math.Pow((double)stdDev, 3)),
                2);
            var kurtosis = Math.Round(
                (decimal)(numbers.Select(x => Math.Pow((double)(x - mean), 4)).Sum() / count / Math.Pow((double)stdDev, 4) - 3),
                2);

            var buckets = PercentChangeFrequencies.Calculate(
                numbers,
                min,
                max
            );

            return new DistributionStatistics(
                count: count,
                kurtosis: kurtosis,
                min: min,
                max: max,
                mean: mean,
                median: median,
                skewness: skewness,
                stdDev: stdDev,
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

    public record struct DistributionStatistics(
        decimal count,
        decimal kurtosis,
        decimal min,
        decimal max,
        decimal mean,
        decimal median,
        decimal skewness,
        decimal stdDev,
        PercentChangeFrequency[] buckets);
    public record struct PercentChangeFrequency(int percentChange, int frequency);
}