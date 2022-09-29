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
            foreach (var p in prices.Take(interval))
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
            var smas = SMA.Generate(prices);

            // add all smas to outcomes
            foreach (var sma in smas)
            {
                outcomes.Add(
                    new AnalysisOutcome(
                        OutcomeKeys.SMA(sma.Interval),
                        OutcomeType.Neutral,
                        sma.Values.Last().Value,
                        $"SMA {sma.Interval} is {sma.Values.Last()}"
                    )
                );
            }

            // positive SMA is if each interval is higher than the next
            var positiveSMA = currentPrice > smas[0].Values.Last().Value;
            for (var i = 0; i < smas.Length - 1; i++)
            {
                var current = smas[i];
                var next = smas[i + 1];
                if (current.Values.Last() < next.Values.Last())
                {
                    positiveSMA = false;
                    break;
                }
            }

            var negativeSMA = currentPrice < smas[0].Values.Last().Value;
            for (var i = 0; i < smas.Length - 1; i++)
            {
                var current = smas[i];
                var next = smas[i + 1];
                if (current.Values.Last() > next.Values.Last())
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
                    "SMA sequence is positive"
                )
            );

            return outcomes;
        }

        private static decimal Round(double value, int digits) => 
            (decimal)Math.Round(value, digits);

        private static decimal Round(decimal value, int digits) => 
            Math.Round(value, digits);
    }

    internal class OutcomeKeys
    {
        public static string LowestPrice = "LowestPrice";
        public static string LowestPriceDaysAgo = "LowestPriceDaysAgo";
        public static string HighestPrice = "HighestPrice";
        public static string HighestPriceDaysAgo = "HighestPriceDaysAgo";
        public static string AverageVolume = "AverageVolume";
        public static string SMASequence = "SMASequence";

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

        private static readonly IEnumerable<int> _smaIntervals = new [] {20, 50, 150, 200};
        public static SMA[] Generate(HistoricalPrice[] prices)
        {
            return _smaIntervals.Select(interval => ToSMA(prices, interval)).ToArray();
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
    }

    internal enum OutcomeType { Positive, Negative, Neutral };

    internal record AnalysisOutcome(string key, OutcomeType type, decimal value, string message);
}