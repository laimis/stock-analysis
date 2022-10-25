using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public interface ISingleBarPriceAnalysis
    {
        IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices);
    }

    public class SingleBarAnalysisConstants
    {
        public static int NumberOfDaysForRecentAnalysis = 60;
    }

    public class SingleBarAnalysisRunner
    {
        public static List<AnalysisOutcome> Run(HistoricalPrice[] prices)
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
        public IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices)
        {
            var price = prices[prices.Length - 1].Close;
            
            var outcomes = new SMAAnalysis().Run(price, prices);

            var outcome = outcomes.Single(x => x.key == HistoricalOutcomeKeys.SMA20Above50Days);

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
        public IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices)
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

            // today's change from high to low
            var change = Math.Round((currentBar.High - currentBar.Low) / currentBar.Low * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.HighToLowChangeDay,
                type: OutcomeType.Neutral,
                value: change,
                message: $"Day change from high to low is {change}.");

            // today's change from open to close
            change = Math.Round((currentBar.Close - currentBar.Open) / currentBar.Open * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.OpenToCloseChangeDay,
                type: change >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                value: change,
                message: $"Day change from open to close is {change}.");

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

            // today's true range
            var trueRange = Math.Round((currentBar.Close - trueLow) / (trueHigh - trueLow) * 100, 2);

            // add true range as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.TrueRange,
                type: trueRange >= 80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: trueRange,
                message: $"True range is {trueRange}.");

            // see if there was a gap down or gap up
            var gap = 0m;

            if (currentBar.Low > yesterday.High)
            {
                gap = Math.Round( (currentBar.Low - yesterday.High)/yesterday.High * 100, 2);
            }
            else if (currentBar.High < yesterday.Low)
            {
                gap = -1 * Math.Round( (yesterday.Low - currentBar.High)/yesterday.Low * 100, 2);
            }

            var gapType = gap switch {
                > 0m => OutcomeType.Positive,
                < 0m => OutcomeType.Negative,
                _ => OutcomeType.Neutral
            };

            // add gap as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.GapPercentage,
                type: gapType,
                value: gap,
                message: $"Gap is {gap}%.");

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
            var descriptor = PercentChangeAnalysis.Generate(prices.Skip(prices.Length - SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis).ToArray());

            var sigmaRatio = percentChange switch {
                >=0 => percentChange / (descriptor.average + descriptor.standardDeviation),
                <0 => percentChange / (descriptor.average - descriptor.standardDeviation)
            };

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
        public IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices)
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
            var averageVolume = 0m;
            var interval  = Math.Min(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis, prices.Length);
            for (var i = prices.Length - interval; i < prices.Length; i++)
            {
                averageVolume += prices[i].Volume;
            }
            averageVolume /= interval;

            // calculate today's relative volume
            var relativeVolume = Math.Round(last.Volume / averageVolume, 2);

            var priceDirection = last.Close > last.Open
                ? OutcomeType.Positive : OutcomeType.Negative;

            // add relative volume as outcome
            outcomes.Add(new AnalysisOutcome(
                key: SingleBarOutcomeKeys.RelativeVolume,
                type: relativeVolume >= 0.9m ? priceDirection : OutcomeType.Neutral,
                value: relativeVolume,
                message: $"Relative volume is {relativeVolume}x the average volume over the last {interval} days."
            ));

            return outcomes;
        }
    }

    internal class SingleBarOutcomeKeys
    {
        public static string RelativeVolume = "RelativeVolume";
        public static string Volume = "Volume";
        public static string TrueRange = "TrueRange";
        public static string PercentChange = "PercentChange";
        public static string OpenToCloseChangeDay = "OpenToCloseChangeDay";
        public static string HighToLowChangeDay = "HighToLowChangeDay";
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