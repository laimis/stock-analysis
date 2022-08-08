namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            TradingEntryView[] current,
            PositionInstance[] past,
            BrokerageOrderView[] pendingOrders,
            TradingPerformanceContainerView performance)
        {
            Current = current;
            Past = past;
            PendingOrders = pendingOrders;
            Performance = performance;
        }

        public TradingEntryView[] Current { get; }
        public PositionInstance[] Past { get; }
        public BrokerageOrderView[] PendingOrders { get; }
        public TradingPerformanceContainerView Performance { get; }
    }
}