using System.Collections.Generic;
using core.Shared.Adapters.Brokerage;

namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            Order[] brokerageOrders,
            PositionInstance[] current,
            PositionInstance[] past,
            TradingPerformanceContainerView performance,
            List<StockViolationView> violations)
        {
            Current = current;
            Past = past;
            BrokerageOrders = brokerageOrders;
            Performance = performance;
            Violations = violations;
        }

        public Order[] BrokerageOrders { get; }
        public PositionInstance[] Current { get; }
        public PositionInstance[] Past { get; }
        public TradingPerformanceContainerView Performance { get; }
        public List<StockViolationView> Violations { get; }
    }
}