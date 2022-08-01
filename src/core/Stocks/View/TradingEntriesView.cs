namespace core.Stocks.View
{
    public class TradingEntriesView
    {
        public TradingEntriesView(
            TradingEntryView[] current,
            PositionInstance[] past,
            PendingOrderView[] pendingOrders,
            TradingPerformanceContainerView performance)
        {
            Current = current;
            Past = past;
            PendingOrders = pendingOrders;
            Performance = performance;
        }

        public TradingEntryView[] Current { get; }
        public PositionInstance[] Past { get; }
        public PendingOrderView[] PendingOrders { get; }
        public TradingPerformanceContainerView Performance { get; }
    }
}