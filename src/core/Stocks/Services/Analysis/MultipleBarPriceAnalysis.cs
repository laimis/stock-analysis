#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit {}
}

namespace core.Stocks.Services.Analysis
{
    internal interface IMultipleBarPriceAnalysis
    {
        IEnumerable<AnalysisOutcome> Run(decimal currentPrice, PriceBar[] prices);
    }

    public class MultipleBarPriceAnalysis
    {
        public static List<AnalysisOutcome> Run(decimal currentPrice, PriceBar[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            outcomes.AddRange(new PriceAnalysis().Run(currentPrice, prices));
            outcomes.AddRange(new VolumeAnalysis().Run(currentPrice, prices));
            outcomes.AddRange(new SMAAnalysis().Run(currentPrice, prices));

            return outcomes;
        }
    }

    internal class PriceAnalysis : IMultipleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Run(decimal currentPrice, PriceBar[] prices)
        {
            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.CurrentPrice,
                OutcomeType.Neutral,
                currentPrice,
                $"Current price is {currentPrice:C2}"
            );

            var lowest = prices[0];
            var highest = prices[0];
            foreach (var p in prices)
            {
                if (p.Close < lowest.Close)
                {
                    lowest = p;
                }

                if (p.Close > highest.Close)
                {
                    highest = p;
                }
            }

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.LowestPrice,
                OutcomeType.Neutral,
                lowest.Close,
                $"Lowest price was {lowest.Close} on {lowest.Date}"
            );

             // if low is within 30 days, theb add negative outcome
            var lowestPriceDaysAgo =  (decimal)Math.Round(DateTimeOffset.Now.Subtract(lowest.Date).TotalDays, 0);
            var lowestPriceDaysAgoOutcomeType = lowestPriceDaysAgo <= 30 ? OutcomeType.Negative : OutcomeType.Neutral;
            
            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.LowestPriceDaysAgo,
                lowestPriceDaysAgoOutcomeType,
                lowestPriceDaysAgo,
                $"Lowest price was {lowest.Close} on {lowest.Date} which was {lowestPriceDaysAgo} days ago"
            );

            var percentAboveLow = (decimal)Math.Round((currentPrice - lowest.Close) / lowest.Close * 100, 2);
            var percentAboveLowOutcomeType = OutcomeType.Neutral;
            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.PercentAbovLow,
                    percentAboveLowOutcomeType,
                    percentAboveLow,
                    $"Percent above recent low: {percentAboveLow}%"
                );

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.HighestPrice,
                OutcomeType.Neutral,
                highest.Close,
                $"Highest price was {highest.Close} on {highest.Date}"
            );

            // if high is within 30 days, theb add negative outcome
            var highestPriceDaysAgo =  (decimal)Math.Round(DateTimeOffset.Now.Subtract(highest.Date).TotalDays, 0);
            var highestPriceDaysAgoOutcomeType = highestPriceDaysAgo <= 30 ? OutcomeType.Positive : OutcomeType.Neutral;
            
            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.HighestPriceDaysAgo,
                highestPriceDaysAgoOutcomeType,
                highestPriceDaysAgo,
                $"Highest price was {highest.Close} on {highest.Date} which was {highestPriceDaysAgo} days ago"
            );

            var percentBelowHigh = (decimal)Math.Round((highest.Close - currentPrice) / highest.Close * 100, 2);
            var percentBelowHighOutcomeType = OutcomeType.Neutral;
            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.PercentBelowHigh,
                    percentBelowHighOutcomeType,
                    percentBelowHigh,
                    $"Percent below recent high: {percentBelowHigh}%"
                );

            // count gap ups and gap downs
            var gapResults = GapAnalysis.Generate(
                prices,
                MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis);

            var gapUps = gapResults.Count(g => g.type == GapType.Up && g.open);
            var gapDowns = gapResults.Count(g => g.type == GapType.Down && g.open);

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.GapUps,
                OutcomeType.Neutral,
                gapUps,
                $"Open gap ups: {gapUps}"
            );

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.GapDowns,
                OutcomeType.Neutral,
                gapDowns,
                $"open gap downs: {gapDowns}"
            );

            // statistical analysis bits of percent changes
            var descriptor = NumberAnalysis.PercentChanges(
                prices
                .Skip(prices.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                .Select(p => p.Close)
                .ToArray());

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.PercentChangeAverage,
                OutcomeType.Neutral,
                descriptor.mean,
                $"% Change Average: {descriptor.mean}"
            );

            yield return new AnalysisOutcome(
                MultipleBarOutcomeKeys.PercentChangeStandardDeviation,
                OutcomeType.Neutral,
                descriptor.stdDev,
                $"% Change StD: {descriptor.stdDev}"
            );
        }
    }

    internal class VolumeAnalysis : IMultipleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Run(decimal price, PriceBar[] prices)
        {
            // find average volume over the last x days
            var totalVolume = 0m;
            var interval = Math.Min(MultipleBarPriceAnalysisConstants.NumberOfDaysForRecentAnalysis, prices.Length);
            for (var i = prices.Length - interval; i < prices.Length; i++)
            {
                totalVolume += prices[i].Volume;
            }
            var averageVolume = (decimal)Math.Round(totalVolume / interval, 0);

            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.AverageVolume,
                    OutcomeType.Neutral,
                    averageVolume,
                    $"Average volume over the last {interval} days is {averageVolume}"
                );
        }
    }

    internal class SMAAnalysis : IMultipleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Run(decimal currentPrice, PriceBar[] prices)
        {
            // generate SMAs
            var smaContainer = SMAContainer.Generate(prices);

            // add all smas to outcomes
            foreach (var sma in smaContainer.GetEnumerable())
            {
                var value = sma.LastValue;
                yield return
                    new AnalysisOutcome(
                        MultipleBarOutcomeKeys.SMA(sma.Interval),
                        OutcomeType.Neutral,
                        Math.Round(value ?? 0, 2),
                        $"SMA {sma.Interval} is {value}"
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

            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.SMASequence,
                    smaSequenceOutcomeType,
                    smaSequenceValue,
                    $"SMA sequence is {smaSequenceOutcomeType.ToString()}"
                );

            // 20 is below 50, negative
            var sma20Above50Diff = Math.Round((smaContainer.LastValueOfSMA(0) - smaContainer.LastValueOfSMA(1) ?? 0), 2);
            var sma20Above50 = sma20Above50Diff >= 0;
            var sma20Above50OutcomeType = sma20Above50 ? OutcomeType.Positive : OutcomeType.Negative;
            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.SMA20Above50,
                    sma20Above50OutcomeType,
                    sma20Above50Diff,
                    $"SMA 20 - SMA 50: {sma20Above50Diff}"
                );

            // 50 is below 150, negative
            var sma50Above150Diff = Math.Round((smaContainer.LastValueOfSMA(1) - smaContainer.LastValueOfSMA(2) ?? 0), 2);
            var sma50Above150 = sma50Above150Diff > 0;
            var sma50Above150OutcomeType = sma50Above150 ? OutcomeType.Positive : OutcomeType.Negative;
            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.SMA50Above150,
                    sma50Above150OutcomeType,
                    sma50Above150Diff,
                    $"SMA 50 - SMA 150: {sma50Above150Diff}"
                );

            // how many days is 20 below 50
            var sma20Below50Days = 0;
            var sma20 = smaContainer.sma20;
            var sma50 = smaContainer.sma50;
            for (var i = sma20.Values.Length - 1; i >= 0; i--)
            {
                if (sma20.Values[i] < sma50.Values[i])
                {
                    sma20Below50Days++;
                }
                else
                {
                    break;
                }
            }

            var sma20Above50Days = 0;
            for (var i = sma20.Values.Length - 1; i >= 0; i--)
            {
                if (sma20.Values[i] > sma50.Values[i])
                {
                    sma20Above50Days++;
                }
                else
                {
                    break;
                }
            }

            var sma20Above50DaysOutcomeType = sma20Below50Days > 0 ? OutcomeType.Negative : OutcomeType.Positive;
            var sma20Above50DaysValue = sma20Below50Days > 0 ? sma20Below50Days * -1 : sma20Above50Days;
            yield return
                new AnalysisOutcome(
                    MultipleBarOutcomeKeys.SMA20Above50Days,
                    sma20Above50DaysOutcomeType,
                    sma20Above50DaysValue,
                    "SMA 20 has been " + (sma20Below50Days > 0 ? "below" : "above") + $" SMA 50 for {sma20Above50DaysValue} days"
                );
        }
    }    

    internal class MultipleBarPriceAnalysisConstants
    {
        internal static int NumberOfDaysForRecentAnalysis = 60;
    }

    internal class MultipleBarOutcomeKeys
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
        public static string SMA20Above50Days = "SMA20Above50Days";
        public static string CurrentPrice = "CurrentPrice";
        public static string GapUps = "OpenGapUps";
        public static string GapDowns = "OpenGapDowns";
        public static string PercentChangeAverage = "PercentChangeAverage";
        public static string PercentChangeStandardDeviation = "PercentChangeStandardDeviation";

        internal static string SMA(int interval) => $"sma_{interval}";
    }
}
#nullable restore