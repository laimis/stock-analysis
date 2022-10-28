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

            var buckets = new Dictionary<int, int>();

            var from  = (int)Math.Floor(min);
            var to = (int)Math.Floor(max);
            var length = to - from + 1;

            foreach(var range in Enumerable.Range((int)Math.Floor(min), length))
            {
                buckets.Add(range, 0);
            }

            foreach(var change in percentChanges)
            {
                var bucket = (int)Math.Floor(change);
                buckets[bucket]++;
            }

            return new PercentChangeDescriptor(
                stdDev,
                min,
                max,
                avg,
                buckets.Select(kp => new PercentChangeFrequency(kp.Key, kp.Value)).OrderBy(f => f.percentChange).ToArray()
            );
        }        
    }

    public record struct PercentChangeDescriptor(decimal standardDeviation, decimal min, decimal max, decimal average, PercentChangeFrequency[] buckets);
    public record struct PercentChangeFrequency(int percentChange, int frequency);
}