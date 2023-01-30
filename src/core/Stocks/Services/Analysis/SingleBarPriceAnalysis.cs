using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class SingleBarAnalysisConstants
    {
        public static int NumberOfDaysForRecentAnalysis = 60;
    }

    public class SingleBarAnalysisRunner
    {
        public static List<AnalysisOutcome> Run(PriceBar currentBar, PriceBar[] previousBars)
        {
            var outcomes = new List<AnalysisOutcome>();

            outcomes.AddRange(new SingleBarVolumeAnalysis().Analyze(currentBar, previousBars));
            outcomes.AddRange(new SingleBarPriceAnalysis().Analyze(currentBar, previousBars));
            outcomes.AddRange(new SMASingleBarAnalysis().Analyze(currentBar, previousBars));

            return outcomes;
        }
    }

    internal class SMASingleBarAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar currentBar, PriceBar[] previousBars)
        {
            var price = currentBar.Close;
            
            var outcomes = new SMAAnalysis().Run(price, previousBars);

            var outcome = outcomes.Single(x => x.key == MultipleBarOutcomeKeys.SMA20Above50Days);

            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.SMA20Above50Days,
                outcome.type,
                outcome.value,
                outcome.valueType,
                outcome.message
            );
        }
    }

    internal class SingleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(
            PriceBar currentBar,
            PriceBar[] previousBars)
        {
            // return open as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Open,
                OutcomeType.Neutral,
                currentBar.Open,
                OutcomeValueType.Currency,
                "Open price");

            // return close as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Close,
                OutcomeType.Neutral,
                currentBar.Close,
                OutcomeValueType.Currency,
                "Close price");

            var range = currentBar.ClosingRange();
            
            // add range as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.ClosingRange,
                type: range >= 0.80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: range,
                valueType: OutcomeValueType.Percentage,
                message: $"Closing range is {range}.");

            // use yesterday's close as reference
            var yesterday = previousBars[^1];

            // today's change from yesterday's close
            var percentChange = (currentBar.Close - yesterday.Close) / yesterday.Close;

            // add change as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.PercentChange,
                type: percentChange >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                value: percentChange,
                valueType: OutcomeValueType.Percentage,
                message: $"% change from close is {percentChange}.");

            // true range uses the previous close as reference
            var trueHigh = Math.Max(currentBar.High, yesterday.Close);
            var trueLow = Math.Min(currentBar.Low, yesterday.Close);

            // see if there was a gap down or gap up
            var gaps = GapAnalysis.Generate(previousBars, SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis);
            var gap = gaps.FirstOrDefault(
                x => x.bar.Equals(currentBar)
            );

            var gapType = gap.gapSizePct switch {
                > 0m => OutcomeType.Positive,
                < 0m => OutcomeType.Negative,
                _ => OutcomeType.Neutral
            };

            // add gap as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.GapPercentage,
                type: gapType,
                value: gap.gapSizePct,
                valueType: OutcomeValueType.Percentage,
                message: $"Gap is {gap.gapSizePct}%.");

            // see if the latest bar is a one year high or low
            var oneYearAgoDate = currentBar.Date.AddYears(-1);
            var newHigh = previousBars
                .Where(b => b.Date >= oneYearAgoDate)
                .All(x => x.High < currentBar.High);
            var newLow = previousBars
                .Where(b => b.Date >= oneYearAgoDate)
                .All(x => x.Low > currentBar.Low);

            // add new high as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewHigh,
                type: newHigh ? OutcomeType.Positive : OutcomeType.Neutral,
                value: newHigh ? 1m : 0m,
                valueType: OutcomeValueType.Boolean,
                message: $"New high reached");

            // add new low as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewLow,
                type: newLow ? OutcomeType.Negative : OutcomeType.Neutral,
                value: newLow ? -1m : 0m,
                valueType: OutcomeValueType.Boolean,
                message: $"New low reached");

            // generate percent change statistical data for recent days
            var descriptor = NumberAnalysis.PercentChanges(
                previousBars.Last(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                .Select(x => x.Close)
                .ToArray());

            // for some price feeds, price has finished changing, so mean and
            // stdev will be 0, we need to check for that so that we don't divide by 0
            var sigmaRatioDenominotor = percentChange switch {
                >=0 => descriptor.mean + descriptor.stdDev,
                <0 => descriptor.mean - descriptor.stdDev
            };    
            var sigmaRatio = sigmaRatioDenominotor == 0 ? 0 :  percentChange switch {
                >=0 => percentChange / sigmaRatioDenominotor,
                <0 => -1m * (percentChange / sigmaRatioDenominotor)
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
                valueType: OutcomeValueType.Number,
                message: $"Sigma ratio is {sigmaRatio}.");
        }
    }

    internal class SingleBarVolumeAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar last, PriceBar[] previousBars)
        {
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.Volume,
                type: OutcomeType.Neutral,
                value: last.Volume,
                valueType: OutcomeValueType.Number,
                message: "Volume");

            // calculate today's relative volume
            var volumeStats = NumberAnalysis.Statistics(
                previousBars
                    .Last(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                    .Select(x => x.Volume)
                    .ToArray()
            );

            var relativeVolume = Math.Round(last.Volume / volumeStats.mean, 2);

            var priceDirection = last.Close > last.Open
                ? OutcomeType.Positive : OutcomeType.Negative;

            // add relative volume as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.RelativeVolume,
                type: relativeVolume >= 0.9m ? priceDirection : OutcomeType.Neutral,
                value: relativeVolume,
                valueType: OutcomeValueType.Number,
                message: $"Relative volume is {relativeVolume}x the average volume over the last {volumeStats.count} days."
            );
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
        public static string Highlight = "Highlight";
    }
}