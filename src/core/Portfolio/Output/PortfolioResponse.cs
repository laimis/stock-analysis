namespace core.Portfolio.Output
{
    public class PortfolioResponse
    {
        public int OwnedStockCount { get; internal set; }
        public int OpenOptionCount { get; internal set; }
        public int TriggeredAlertCount { get; internal set; }
        public int AlertCount { get; internal set; }
    }
}