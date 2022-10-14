using System;
using System.Collections.Generic;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services
{
    public interface IDailyPriceAnalysis
    {
        IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices);
    }

    public class DailyPriceAnalysisRunner
    {
        public static List<AnalysisOutcome> Run(HistoricalPrice[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            outcomes.AddRange(new DailyVolumeAnalysis().Analyze(prices));
            outcomes.AddRange(new DailyPriceAnalysis().Analyze(prices));

            return outcomes;
        }
    }

    internal class DailyPriceAnalysis : IDailyPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices)
        {
            var last = prices[prices.Length - 1];

            // return open as the neutral outcome
            yield return new AnalysisOutcome("Open", OutcomeType.Neutral, last.Open, "Open price");

            // return close as the neutral outcome
            yield return new AnalysisOutcome("Close", OutcomeType.Neutral, last.Close, "Close price");

            // calculate closing range
            var range = Math.Round((last.Close - last.Low) / (last.High - last.Low) * 100, 2);

            // add range as outcome
            yield return new AnalysisOutcome(
                key: "ClosingRange",
                type: range >= 80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: range,
                message: $"Closing range is {range}.");

            // today's change from high to low
            var change = Math.Round((last.High - last.Low) / last.Low * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: "HighToLowChangeDay",
                type: OutcomeType.Neutral,
                value: change,
                message: $"Day change from high to low is {change}.");

            // today's change from open to close
            change = Math.Round((last.Close - last.Open) / last.Open * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: "OpenToCloseChangeDay",
                type: change >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                value: change,
                message: $"Day change from open to close is {change}.");

            // use yesterday's close as reference
            var yesterday = prices[prices.Length - 2];

            // today's change from yesterday's close
            change = Math.Round((last.Close - yesterday.Close) / yesterday.Close * 100, 2);

            // add change as outcome
            yield return new AnalysisOutcome(
                key: "PercentChange",
                type: change >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                value: change,
                message: $"% change from close is {change}.");

            // true range uses the previous close as reference
            var trueHigh = Math.Max(last.High, yesterday.Close);
            var trueLow = Math.Min(last.Low, yesterday.Close);

            // today's true range
            var trueRange = Math.Round((last.Close - trueLow) / (trueHigh - trueLow) * 100, 2);

            // add true range as outcome
            yield return new AnalysisOutcome(
                key: "TrueRange",
                type: trueRange >= 80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: trueRange,
                message: $"True range is {trueRange}.");   
        }
    }

    internal class DailyVolumeAnalysis : IDailyPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(HistoricalPrice[] prices)
        {
            var outcomes = new List<AnalysisOutcome>();

            var last = prices[prices.Length - 1];

            // add volume as a neutral outcome
            outcomes.Add(new AnalysisOutcome(
                key: "Volume",
                type: OutcomeType.Neutral,
                value: last.Volume,
                message: "Volume"));

            // calculate the average volume from the last x days
            var averageVolume = 0m;
            var interval  = 60;
            for (var i = prices.Length - interval - 1; i < prices.Length; i++)
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
                key: "RelativeVolume",
                type: relativeVolume >= 0.9m ? priceDirection : OutcomeType.Neutral,
                value: relativeVolume,
                message: $"Relative volume is {relativeVolume}x the average volume over the last 30 days."
            ));

            return outcomes;
        }
    }
}