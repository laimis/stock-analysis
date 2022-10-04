#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

namespace core.Stocks.Services
{
    public class StockPriceAnalysis
    {
        internal static List<AnalysisOutcome> Run(decimal currentPrice, HistoricalPrice[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            var lowest = prices[0];
            foreach (var p in prices)
            {
                if (p.Close < lowest.Close)
                {
                    lowest = p;
                }
            }

            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.LowestPrice,
                    OutcomeType.Neutral,
                    lowest.Close,
                    $"Lowest price was {lowest.Close} on {lowest.Date}"
                )
            );

            // if low is within 30 days, theb add negative outcome
            var lowestPriceDaysAgo = Round(DateTimeOffset.Now.Subtract(lowest.DateParsed).TotalDays, 0);
            var lowestPriceDaysAgoOutcomeType = lowestPriceDaysAgo <= 30 ? OutcomeType.Negative : OutcomeType.Neutral;
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.LowestPriceDaysAgo,
                    lowestPriceDaysAgoOutcomeType,
                    lowestPriceDaysAgo,
                    $"Lowest price was {lowest.Close} on {lowest.Date} which was {lowestPriceDaysAgo} days ago"
                )
            );
            
            // find historical price with the highest closing price
            var highest = prices[0];
            foreach (var p in prices)
            {
                if (p.Close > highest.Close)
                {
                    highest = p;
                }
            }

            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.HighestPrice,
                    OutcomeType.Neutral,
                    highest.Close,
                    $"Highest price was {highest.Close} on {highest.Date}"
                )
            );

            // if high is within 30 days, theb add positive outcome
            var highestPriceDaysAgo = Round(DateTimeOffset.Now.Subtract(highest.DateParsed).TotalDays, 0);
            var highestPriceDaysAgoOutcomeType = highestPriceDaysAgo < 30 ? OutcomeType.Positive : OutcomeType.Neutral;
            
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.HighestPriceDaysAgo,
                    OutcomeType.Positive,
                    highestPriceDaysAgo,
                    $"Highest price was {highest.Close} on {highest.Date} which was {highestPriceDaysAgo} days ago"
                )
            );

            // find average volume over the last 30 days
            var totalVolume = 0m;
            var interval = 30;
            foreach (var p in prices.AsSpan().Slice(start: prices.Length - 30))
            {
                totalVolume += p.Volume;
            }
            var averageVolume = Round(totalVolume / interval, 0);

            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.AverageVolume,
                    OutcomeType.Neutral,
                    averageVolume,
                    $"Average volume over the last {interval} days is {averageVolume}"
                )
            );


            // generate SMAs
            var smaContainer = SMAContainer.Generate(prices);

            // add all smas to outcomes
            foreach (var sma in smaContainer.GetEnumerable())
            {
                var value = sma.LastValue;
                outcomes.Add(
                    new AnalysisOutcome(
                        OutcomeKeys.SMA(sma.Interval),
                        OutcomeType.Neutral,
                        Round(value ?? 0, 2),
                        $"SMA {sma.Interval} is {value}"
                    )
                );
            }

            // positive SMA is if each interval is higher than the next
            var positiveSMA = currentPrice > smaContainer.LastValueOfSMA(0);
            for (var i = 0; i < smaContainer.Length - 1; i++)
            {
                var current = smaContainer.LastValueOfSMA(0);
                var next = smaContainer.LastValueOfSMA(i + 1);
                if (current < next)
                {
                    positiveSMA = false;
                    break;
                }
            }

            var negativeSMA = currentPrice < smaContainer.LastValueOfSMA(0);
            for (var i = 0; i < smaContainer.Length - 1; i++)
            {
                var current = smaContainer.LastValueOfSMA(i);
                var next = smaContainer.LastValueOfSMA(i + 1);
                if (current > next)
                {
                    negativeSMA = false;
                    break;
                }
            }

            var (smaSequenceOutcomeType, smaSequenceValue) = (positiveSMA, negativeSMA) switch {
                (true, false) => (OutcomeType.Positive, 1m),
                (false, true) => (OutcomeType.Negative, -1m),
                _ => (OutcomeType.Neutral, 0m)
            };

            var smaSequenceMessage = $"SMA sequence is {smaSequenceOutcomeType.ToString()}";

            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.SMASequence,
                    smaSequenceOutcomeType,
                    smaSequenceValue,
                    $"SMA sequence is {smaSequenceOutcomeType.ToString()}"
                )
            );

            // 20 is below 50, negative
            var sma20Above50Diff = Round(smaContainer.LastValueOfSMA(0) - smaContainer.LastValueOfSMA(1), 2);
            var sma20Above50 = sma20Above50Diff > 0;
            var sma20Above50OutcomeType = sma20Above50 ? OutcomeType.Positive : OutcomeType.Negative;
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.SMA20Above50,
                    sma20Above50OutcomeType,
                    sma20Above50Diff,
                    $"SMA 20 - SMA 50: {sma20Above50Diff}"
                )
            );

            // 50 is below 150, negative
            var sma50Above150Diff = Round(smaContainer.LastValueOfSMA(1) - smaContainer.LastValueOfSMA(2), 2);
            var sma50Above150 = sma50Above150Diff > 0;
            var sma50Above150OutcomeType = sma50Above150 ? OutcomeType.Positive : OutcomeType.Negative;
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.SMA50Above150,
                    sma50Above150OutcomeType,
                    sma50Above150Diff,
                    $"SMA 50 - SMA 150: {sma50Above150Diff}"
                )
            );

            // percent below recent high
            var percentBelowHigh = Round((currentPrice - highest.Close) / highest.Close * 100, 2);
            var percentBelowHighOutcomeType = OutcomeType.Neutral;
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.PercentBelowHigh,
                    percentBelowHighOutcomeType,
                    percentBelowHigh,
                    $"Percent below recent high: {percentBelowHigh}%"
                )
            );

            var percentAboveLow = Round((currentPrice - lowest.Close) / lowest.Close * 100, 2);
            var percentAboveLowOutcomeType = OutcomeType.Neutral;
            outcomes.Add(
                new AnalysisOutcome(
                    OutcomeKeys.PercentAbovLow,
                    percentAboveLowOutcomeType,
                    percentAboveLow,
                    $"Percent above recent low: {percentAboveLow}%"
                )
            );

            return outcomes;
        }

        private static decimal Round(double value, int digits) => 
            (decimal)Math.Round(value, digits);

        private static decimal Round(decimal? value, int digits) => 
            value switch {
                null => 0,
                _ => Math.Round(value.Value, digits)
            };
    }

    internal class OutcomeKeys
    {
        public static string LowestPrice = "LowestPrice";
        public static string LowestPriceDaysAgo = "LowestPriceDaysAgo";
        public static string HighestPrice = "HighestPrice";
        public static string HighestPriceDaysAgo = "HighestPriceDaysAgo";
        public static string AverageVolume = "AverageVolume";
        public static string SMASequence = "SMASequence";
        public static string SMA20Above50 = "SMA20Above50";
        public static string SMA50Above150 = "SMA50Above150";
        public static string PercentBelowHigh = "PercentBelowHigh";
        public static string PercentAbovLow = "PercentAboveLow";

        internal static string SMA(int interval) => $"sma_{interval}";
    }

    public class SMA
    {
        public SMA(decimal?[] values, int interval)
        {
            Interval = interval;
            Values = values;
        }
        
        public int Interval { get; }
        public decimal?[] Values { get; }
        public string Description => $"SMA {Interval}";

        public decimal? LastValue => Values?.Last();
    }

    public class SMAContainer
    {
        private SMA _sma20;
        private SMA _sma50;
        private SMA _sma150;
        private SMA _sma200;
        private SMA[] _all;

        public SMAContainer(SMA sma20, SMA sma50, SMA sma150, SMA sma200)
        {
            _sma20 = sma20;
            _sma50 = sma50;
            _sma150 = sma150;
            _sma200 = sma200;

            _all = new SMA[] { sma20, sma50, sma150, sma200 };
        }

        // public IReadOnlyList<SMA> All => _all;

        public int Length => _all.Length;

        public SMA sma20 => _sma20;
        public SMA sma50 => _sma50;
        public SMA sma150 => _sma150;
        public SMA sma200 => _sma200;


        public static SMAContainer Generate(HistoricalPrice[] prices)
        {
            return new SMAContainer(
                ToSMA(prices, 20),
                ToSMA(prices, 50),
                ToSMA(prices, 150),
                ToSMA(prices, 200)
            );
        }

        private static SMA ToSMA(HistoricalPrice[] prices, int interval)
        {
            var sma = new decimal?[prices.Length];
            for(var i = 0; i<prices.Length; i++)
            {
                if (i < interval)
                {
                    sma[i] = null;
                    continue;
                }

                var sum = 0m;
                for (var j = i - 1; j >= i - interval; j--)
                {
                    sum += prices[j].Close;
                }
                sma[i] = sum / interval;
            }
            return new SMA(sma, interval);
        }

        internal IEnumerable<SMA> GetEnumerable() => _all;

        internal decimal? LastValueOfSMA(int index) => _all[index].LastValue;
    }

    public enum OutcomeType { Positive, Negative, Neutral };

    public record AnalysisOutcome(string key, OutcomeType type, decimal value, string message);
}
#nullable restore