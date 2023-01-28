using System.Collections.Generic;
using System.Linq;

namespace core.Stocks.Services.Analysis
{
    public class SingleBarAnalysisOutcomeEvaluation
    {
        private const decimal RelativeVolumeThresholdPositive = 0.9m;
        private const decimal SigmaRatioThreshold = 1m;
        private const decimal SmallPercentChange = 0.02m;
        private const decimal ExcellentClosingRange = 0.80m;
        private const decimal LowClosingRange = 0.20m;

        internal static IEnumerable<AnalysisOutcomeEvaluation> Evaluate(IEnumerable<TickerOutcomes> tickerOutcomes)
        {
            if (tickerOutcomes.Any(o => o.outcomes.Any(o => o.key == SingleBarOutcomeKeys.Highlight)))
            {
                yield return new AnalysisOutcomeEvaluation(
                    "Highlights",
                    OutcomeType.Neutral,
                    SingleBarOutcomeKeys.Highlight,
                    tickerOutcomes
                        .Where(t => t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.Highlight && o.value == 1))
                        .ToList()
                );
            }

            yield return new AnalysisOutcomeEvaluation(
                "High Volume with Excellent Closing Range and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold)
                    ).ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "Positive gap ups",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.GapPercentage,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value > 0))
                    .ToList()
            );

            // stocks that had above average volume grouping
            yield return new AnalysisOutcomeEvaluation(
                "Above Average Volume and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "Excellent Closing Range and High Percent Change",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.ClosingRange,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold)
                    ).ToList()
            );

            // negative outcome types
            yield return new AnalysisOutcomeEvaluation(
                "Above Average Volume and Negative Percent Change",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value < -1 * SigmaRatioThreshold))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "Low Closing Range",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.ClosingRange,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value < LowClosingRange))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "Above Average Volume but Small Positive Percent Change",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= 0 && o.value < SmallPercentChange))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "High Volume with Low Closing Range and Small Percent Change",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.RelativeVolume,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value <= LowClosingRange)
                        && t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && System.Math.Abs(o.value) < SigmaRatioThreshold)
                    ).ToList()
            );


            yield return new AnalysisOutcomeEvaluation(
                "SMA20 Below SMA50 Recent",
                OutcomeType.Neutral,
                SingleBarOutcomeKeys.SMA20Above50Days,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.SMA20Above50Days && o.value <= 0 && o.value > -5))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "Negative gap downs",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.GapPercentage,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value < 0))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "New Highs",
                OutcomeType.Positive,
                SingleBarOutcomeKeys.NewHigh,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.NewHigh && o.value > 0))
                    .ToList()
            );

            yield return new AnalysisOutcomeEvaluation(
                "New Lows",
                OutcomeType.Negative,
                SingleBarOutcomeKeys.NewLow,
                tickerOutcomes
                    .Where(t =>
                        t.outcomes.Any(o => o.key == SingleBarOutcomeKeys.NewLow && o.value < 0))
                    .ToList()
            );
        }
    }
}