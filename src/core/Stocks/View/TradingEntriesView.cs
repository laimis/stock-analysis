namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            BrokerageOrderView[] brokerageOrders,
            TradingEntryView[] current,
            PositionInstance[] past,
            TradingPerformanceContainerView performance)
        {
            Current = current;
            Past = past;
            BrokerageOrders = brokerageOrders;
            Performance = performance;
        }

        public BrokerageOrderView[] BrokerageOrders { get; }
        public TradingEntryView[] Current { get; }
        public PositionInstance[] Past { get; }
        public TradingPerformanceContainerView Performance { get; }
    }
}