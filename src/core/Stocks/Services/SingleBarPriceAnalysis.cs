using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public interface ISingleBarPriceAnalysis
    {
        IEnumerable<AnalysisOutcome> Analyze(PriceBar[] prices);
    }

    public class SingleBarAnalysisConstants
    {
        public static int NumberOfDaysForRecentAnalysis = 60;
    }

    public class SingleBarAnalysisRunner
    {
        public static List<AnalysisOutcome> Run(PriceBar[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            outcomes.AddRange(new SingleBarVolumeAnalysis().Analyze(prices));
            outcomes.AddRange(new SingleBarPriceAnalysis().Analyze(prices));
            outcomes.AddRange(new SMASingleBarAnalysis().Analyze(prices));

            return outcomes;
        }
    }

    internal class SMASingleBarAnalysis : ISingleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] prices)
        {
            var price = prices[prices.Length - 1].Close;
            
            var outcomes = new SMAAnalysis().Run(price, prices);

            var outcome = outcomes.Single(x => x.key == MultipleBarOutcomeKeys.SMA20Above50Days);

            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.SMA20Above50Days,
                outcome.type,
                outcome.value,
                outcome.message
            );
        }
    }

    internal class SingleBarPriceAnalysis : ISingleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] prices)
        {
            var currentBar = prices[prices.Length - 1];

            // return open as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Open,
                OutcomeType.Neutral,
                currentBar.Open,
                "Open price");

            // return close as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Close,
                OutcomeType.Neutral,
                currentBar.Close,
                "Close price");

            // calculate closing range
            var range = Math.Round((currentBar.Close - currentBar.Low) / (currentBar.High - currentBar.Low) * 100, 2);

            // add range as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.ClosingRange,
                type: range >= 80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: range,
                message: $"Closing range is {range}.");

            // use yesterday's close as reference
            var yesterday = prices[prices.Length - 2];

            // today's change from yesterday's close
            var percentChange = Math.Round((currentBar.Close - yesterday.Close) / yesterday.Close * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.PercentChange,
                type: percentChange >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                value: percentChange,
                message: $"% change from close is {percentChange}.");

            // true range uses the previous close as reference
            var trueHigh = Math.Max(currentBar.High, yesterday.Close);
            var trueLow = Math.Min(currentBar.Low, yesterday.Close);

            // see if there was a gap down or gap up
            var gap = GapAnalysis.Generate(prices, SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis).FirstOrDefault(
                x => x.bar.Equals(currentBar)
            );

            var gapPct = gap.bar.Date switch {
                not null => gap.gapSizePct,
                _ => 0m
            };

            var gapType = gapPct switch {
                > 0m => OutcomeType.Positive,
                < 0m => OutcomeType.Negative,
                _ => OutcomeType.Neutral
            };

            // add gap as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.GapPercentage,
                type: gapType,
                value: gapPct,
                message: $"Gap is {gapPct}%.");

            // see if the latest bar is a new high or new low
            var newHigh = prices.Take(prices.Length - 1).All(x => x.High < currentBar.High);
            var newLow = prices.Take(prices.Length - 1).All(x => x.Low > currentBar.Low);

            // add new high as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewHigh,
                type: newHigh ? OutcomeType.Positive : OutcomeType.Neutral,
                value: newHigh ? 1m : 0m,
                message: $"New high reached");

            // add new low as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewLow,
                type: newLow ? OutcomeType.Negative : OutcomeType.Neutral,
                value: newLow ? -1m : 0m,
                message: $"New low reached");

            // generate percent change statistical data for recent days
            var descriptor = NumberAnalysis.PercentChanges(
                prices.Last(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                .Select(x => x.Close)
                .ToArray());

            var sigmaRatio = percentChange switch {
                >=0 => percentChange / (descriptor.mean + descriptor.stdDev),
                <0 => -1m * (percentChange / (descriptor.mean - descriptor.stdDev))
            };

            sigmaRatio = Math.Round(sigmaRatio, 2);

            // add sigma ratio as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.SigmaRatio,
                type: sigmaRatio switch {
                    > 1m => OutcomeType.Positive,
                    < -1m => OutcomeType.Negative,
                    _ => OutcomeType.Neutral
                },
                value: sigmaRatio,
                message: $"Sigma ratio is {sigmaRatio}.");
        }
    }

    internal class SingleBarVolumeAnalysis : ISingleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            var last = prices[prices.Length - 1];

            // add volume as a neutral outcome
            outcomes.Add(new AnalysisOutcome(
                key: SingleBarOutcomeKeys.Volume,
                type: OutcomeType.Neutral,
                value: last.Volume,
                message: "Volume"));

            // calculate the average volume from the last x days
            var volumeStats = NumberAnalysis.Statistics(
                prices
                    .Last(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                    .Select(x => x.Volume)
                    .ToArray()
            );

            // calculate today's relative volume
            var relativeVolume = Math.Round(last.Volume / volumeStats.mean, 2);

            var priceDirection = last.Close > last.Open
                ? OutcomeType.Positive : OutcomeType.Negative;

            // add relative volume as outcome
            outcomes.Add(new AnalysisOutcome(
                key: SingleBarOutcomeKeys.RelativeVolume,
                type: relativeVolume >= 0.9m ? priceDirection : OutcomeType.Neutral,
                value: relativeVolume,
                message: $"Relative volume is {relativeVolume}x the average volume over the last {volumeStats.count} days."
            ));

            return outcomes;
        }
    }

    internal class SingleBarOutcomeKeys
    {
        public static string RelativeVolume = "RelativeVolume";
        public static string Volume = "Volume";
        public static string PercentChange = "PercentChange";
        public static string ClosingRange = "ClosingRange";
        public static string Open = "Open";
        public static string Close = "Close";
        public static string SMA20Above50Days = "SMA20Above50Days";
        public static string GapPercentage = "GapPercentage";
        public static string NewHigh = "NewHigh";
        public static string NewLow = "NewLow";
        public static string SigmaRatio = "SigmaRatio";
    }
}