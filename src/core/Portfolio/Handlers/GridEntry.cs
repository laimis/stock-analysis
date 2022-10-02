using System.Collections.Generic;
using core.Stocks.Services;

namespace core.Portfolio
{
    public struct GridEntry
    {
        public GridEntry(string ticker, List<AnalysisOutcome> outcomes)
        {
            Outcomes = outcomes;
            Ticker = ticker;
        }

        public List<AnalysisOutcome> Outcomes { get; }
        public string Ticker { get; }
    }
}