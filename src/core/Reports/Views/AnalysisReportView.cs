using System.Collections.Generic;
using core.Stocks.Services;

namespace core.Reports.Views
{
    public struct TickerOutcomes
    {
        public TickerOutcomes(List<AnalysisOutcome> outcomes, string ticker)
        {
            Outcomes = outcomes;
            Ticker = ticker;
        }

        public string Ticker { get; }
        public List<AnalysisOutcome> Outcomes { get; }
    }

    public record struct AnalysisCategoryGrouping(
        string name,
        OutcomeType type,
        string sortColumn,
        List<TickerOutcomes> outcomes
    );

    public record struct AnalysisReportView(
        IEnumerable<AnalysisCategoryGrouping> categories
    );
}