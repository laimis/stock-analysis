using System.Collections.Generic;
using core.Stocks;
using core.Stocks.Services;

namespace core.Portfolio
{
    public struct PositionAnalysisEntry
    {
        public PositionAnalysisEntry(PositionInstance position, List<AnalysisOutcome> outcomes)
        {
            Position = position;
            Outcomes = outcomes;
        }

        public PositionInstance Position { get; }
        public List<AnalysisOutcome> Outcomes { get; }
    }
}