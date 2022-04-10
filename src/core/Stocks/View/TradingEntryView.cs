namespace core.Stocks.Views
{
    public class TradingEntryView
    {
        public TradingEntryView(OwnedStockState owned)
        {
            Owned = owned.Owned;
            AverageCost = owned.AverageCost;
            Ticker = owned.Ticker;
        }

        public decimal Owned { get; }
        public decimal AverageCost { get; }
        public string Ticker { get; }
        public decimal Price { get; private set; }
        public decimal Gain { get; private set; }

        internal void ApplyPrice(decimal price)
        {
            Price = price;
            Gain = Owned * (price - AverageCost);
        }
    }
}