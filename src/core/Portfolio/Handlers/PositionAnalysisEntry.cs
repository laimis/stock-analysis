using System.Collections.Generic;
using core.Stocks;
using core.Stocks.Services;

namespace core.Portfolio
{
    public struct TickerAnalysisEntry
    {
        public TickerAnalysisEntry(List<AnalysisOutcome> outcomes, string ticker)
        {
            Outcomes = outcomes;
            Ticker = ticker;
        }

        public string Ticker { get; }
        public List<AnalysisOutcome> Outcomes { get; }
    }
}