using System.Collections.Generic;

namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            BrokerageOrderView[] brokerageOrders,
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

        public BrokerageOrderView[] BrokerageOrders { get; }
        public PositionInstance[] Current { get; }
        public PositionInstance[] Past { get; }
        public TradingPerformanceContainerView Performance { get; }
        public List<StockViolationView> Violations { get; }
    }
}