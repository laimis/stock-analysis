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
        public static List<AnalysisOutcome> Run(PriceBar[] bars)
        {
            var outcomes = new List<AnalysisOutcome>();

            outcomes.AddRange(new SingleBarVolumeAnalysis().Analyze(bars));
            outcomes.AddRange(new SingleBarPriceAnalysis().Analyze(bars));
            outcomes.AddRange(new SMASingleBarAnalysis().Analyze(bars));

            return outcomes;
        }
    }

    internal class SMASingleBarAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] bars)
        {
            var currentBar = bars[^1];

            var price = currentBar.Close;
            
            var outcomes = new SMAAnalysis().Run(price, bars[..^1]);

            var sma20above50outcome = outcomes.SingleOrDefault(x => x.key == MultipleBarOutcomeKeys.SMA20Above50Days);

            if (sma20above50outcome != null)
            {
                yield return new AnalysisOutcome(
                    SingleBarOutcomeKeys.SMA20Above50Days,
                    sma20above50outcome.type,
                    sma20above50outcome.value,
                    sma20above50outcome.valueType,
                    sma20above50outcome.message
                );
            }

            // % difference between sma 20 and price
            var sma20outcome = outcomes.SingleOrDefault(x => x.key == MultipleBarOutcomeKeys.SMA(20));
            if (sma20outcome != null && sma20outcome.value != 0)
            {
                var pctDiff = (price - sma20outcome.value) / sma20outcome.value;

                yield return new AnalysisOutcome(
                    SingleBarOutcomeKeys.PriceAbove20SMA,
                    pctDiff >= 0 ? OutcomeType.Positive : OutcomeType.Negative,
                    pctDiff,
                    Shared.ValueFormat.Percentage,
                    "Percentage that price is above 20 day SMA"
                );
            }
        }
    }

    internal class SingleBarPriceAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] bars)
        {

            var currentBar = bars[^1];
            var previousBars = bars[..^1];

            // return open as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Open,
                OutcomeType.Neutral,
                currentBar.Open,
                Shared.ValueFormat.Currency,
                "Open price");

            // return close as the neutral outcome
            yield return new AnalysisOutcome(
                SingleBarOutcomeKeys.Close,
                OutcomeType.Neutral,
                currentBar.Close,
                Shared.ValueFormat.Currency,
                "Close price");

            var range = currentBar.ClosingRange();
            
            // add range as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.ClosingRange,
                type: range >= 0.80m ? OutcomeType.Positive : OutcomeType.Neutral,
                value: range,
                valueType: Shared.ValueFormat.Percentage,
                message: $"Closing range is {range}.");

            if (previousBars.Length > 0)
            {
                // use yesterday's close as reference
                var yesterday = previousBars[^1];

                // today's change from yesterday's close
                var percentChange = (currentBar.Close - yesterday.Close) / yesterday.Close;

                // add change as outcome
                yield return new AnalysisOutcome(
                    key: SingleBarOutcomeKeys.PercentChange,
                    type: percentChange >= 0m ? OutcomeType.Positive : OutcomeType.Negative,
                    value: percentChange,
                    valueType: Shared.ValueFormat.Percentage,
                    message: $"% change from close is {percentChange}.");

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
                    valueType: Shared.ValueFormat.Number,
                    message: $"Sigma ratio is {sigmaRatio}.");
            }

            // see if there was a gap down or gap up
            var gaps = GapAnalysis.Generate(
                bars,
                SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis
            );
            
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
                valueType: Shared.ValueFormat.Percentage,
                message: $"Gap is {gap.gapSizePct}%.");

            // see if the latest bar is a one year high or low
            var oneYearAgoDate = currentBar.Date.AddYears(-1);
            var oneYearAgoIndex = previousBars
                .Select((x, i) => new { x, i })
                .Where(x => x.x.Date <= oneYearAgoDate)
                .Select(x => x.i)
                .LastOrDefault();
            // now create a new high sequence from that bar
            
            var oneYearAgoBars = bars[oneYearAgoIndex..];

            var newHighsSequence = oneYearAgoBars.Aggregate(
                seed: new List<PriceBar>{bars[oneYearAgoIndex]},
                func: (acc, x) => {
                    var last = acc[^1];
                    if (x.High > last.High) {
                        acc.Add(x);
                    }
                    return acc;
                }
            );
            
            // new high will get triggered only if it's at least 2 months ago
            var newHighSequenceLastBar = newHighsSequence[^1];
            var dayDifferenceBetweenBars = 0d;
            if (newHighsSequence.Count >= 2)
            {
                var newHighSequenceTwoBarsAgo = newHighsSequence[^2];
                dayDifferenceBetweenBars = (newHighSequenceLastBar.Date - newHighSequenceTwoBarsAgo.Date).TotalDays;
            }
            var newHigh = newHighSequenceLastBar.Equals(currentBar)
                && dayDifferenceBetweenBars > 60;

            var newLow = previousBars
                .Where(b => b.Date >= oneYearAgoDate)
                .All(x => x.Low > currentBar.Low);

            // add new high as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewHigh,
                type: newHigh ? OutcomeType.Positive : OutcomeType.Neutral,
                value: newHigh ? 1m : 0m,
                valueType: Shared.ValueFormat.Boolean,
                message: $"New high reached");

            // add new low as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.NewLow,
                type: newLow ? OutcomeType.Negative : OutcomeType.Neutral,
                value: newLow ? -1m : 0m,
                valueType: Shared.ValueFormat.Boolean,
                message: $"New low reached");
        }
    }

    internal class SingleBarVolumeAnalysis
    {
        public IEnumerable<AnalysisOutcome> Analyze(PriceBar[] bars)
        {
            var currentBar = bars[^1];

            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.Volume,
                type: OutcomeType.Neutral,
                value: currentBar.Volume,
                valueType: Shared.ValueFormat.Number,
                message: "Volume");

            // calculate today's relative volume
            var volumeStats = NumberAnalysis.Statistics(
                bars[..^1]
                    .Last(SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis)
                    .Select(x => (decimal)x.Volume)
                    .ToArray()
            );

            var relativeVolume = volumeStats.mean switch {
                0 => 0,
                _ => Math.Round(currentBar.Volume / volumeStats.mean, 2)
            };
            
            var priceDirection = currentBar.Close > currentBar.Open
                ? OutcomeType.Positive : OutcomeType.Negative;

            // add relative volume as outcome
            yield return new AnalysisOutcome(
                key: SingleBarOutcomeKeys.RelativeVolume,
                type: relativeVolume >= 0.9m ? priceDirection : OutcomeType.Neutral,
                value: relativeVolume,
                valueType: Shared.ValueFormat.Number,
                message: $"Relative volume is {relativeVolume}x the average volume over the last {volumeStats.count} days."
            );
        }
    }

    public class SingleBarOutcomeKeys
    {
        public static string PriceAbove20SMA = "PriceAbove20SMA";
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