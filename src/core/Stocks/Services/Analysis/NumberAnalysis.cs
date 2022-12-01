using System;
using System.Linq;

namespace core.Stocks.Services.Analysis
{
    public class NumberAnalysis
    {
        public static DistributionStatistics PercentChanges(Span<decimal> numbers, bool multipleByHundred = false)
        {
            var percentChanges = new decimal[numbers.Length - 1];
            for(var i = 1; i < numbers.Length; i++)
            {
                var percentChange = (numbers[i] - numbers[i-1])/numbers[i-1];
                if (multipleByHundred)
                {
                    percentChange *= 100;
                    Math.Round(percentChange, 2);
                }

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

            var buckets = Histogram.Calculate(
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

    internal class Histogram
    {
        internal static ValueWithFrequency[] Calculate(
            decimal[] numbers,
            decimal min,
            decimal max,
            int numberOfBuckets = 21)
        {
            // calculate boundaries as 21 buckets from min to max
            var result = new ValueWithFrequency[numberOfBuckets];
            var bucketSize = (max - min) / numberOfBuckets;

            // if the bucket size is really small, use just two decimal places
            // otherwise use 0 decimal places
            if (bucketSize < 1)
            {
                bucketSize = Math.Round(bucketSize, 4);
            }
            else
            {
                bucketSize = Math.Floor(bucketSize);
                min = Math.Floor(min);
            }
            
            for(var i = 0; i < numberOfBuckets; i++)
            {
                result[i] = new ValueWithFrequency{ value = min + (i * bucketSize), frequency = 0 };
            }

            foreach(var percentChange in numbers)
            {
                if (percentChange > result[^1].value)
                {
                    result[^1] = result[^1] with { frequency = result[^1].frequency + 1 };
                    continue;
                }

                for(var i = 0; i < numberOfBuckets; i++)
                {
                    if(percentChange <= result[i].value)
                    {
                        result[i] = result[i] with { frequency = result[i].frequency + 1 };
                        break;
                    }
                }
            }
            
            return result;
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
        ValueWithFrequency[] buckets);
    public record struct ValueWithFrequency(decimal value, int frequency);
}