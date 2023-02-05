using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Stocks;

namespace core.Stocks.Services.Analysis
{
    public class SingleBarDailyScoring
    {
        public static List<DateScorePair> Generate(
            PriceBar[] bars, 
            DateTimeOffset start,
            string ticker)
        {
            var indexOfFirstBar = 0;
            foreach(var bar in bars)
            {
                // greater check in there in case the start is not a trading date
                if (bar.Date.Date >= start.Date)
                {
                    break;
                }

                indexOfFirstBar++;
            }

            return Enumerable.Range(indexOfFirstBar, bars.Length - indexOfFirstBar)
                .Select(index => {
                    var barsToUse = bars[..(index+1)];

                    var currentBar = barsToUse[^1];

                    var outcomes = SingleBarAnalysisRunner.Run(barsToUse);
                    var tickerOutcomes = new TickerOutcomes(outcomes, ticker);
                    var evaluations = SingleBarAnalysisOutcomeEvaluation.Evaluate(new[]{tickerOutcomes});
                    var counts = AnalysisOutcomeEvaluationScoringHelper.Generate(evaluations);
                    return new DateScorePair(currentBar.Date, counts.GetValueOrDefault(ticker, 0));
                })
                .ToList();
        }
    }
}