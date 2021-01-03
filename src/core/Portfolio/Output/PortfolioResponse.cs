namespace core.Portfolio.Output
{
    public class PortfolioResponse
    {
        public int OwnedStockCount { get; set; }
        public int OpenOptionCount { get; set; }
        public int TriggeredAlertCount { get; set; }
        public int AlertCount { get; set; }
    }
}