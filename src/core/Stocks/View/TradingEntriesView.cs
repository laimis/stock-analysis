using System.Collections.Generic;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            PositionInstance[] current,
            PositionInstance[] past,
            TradingPerformanceContainerView performance,
            List<StockViolationView> violations)
        {
            Current = current;
            Past = past;
            Performance = performance;
            Violations = violations;
        }

        public PositionInstance[] Current { get; }
        public PositionInstance[] Past { get; }
        public TradingPerformanceContainerView Performance { get; }
        public List<StockViolationView> Violations { get; }
    }
}