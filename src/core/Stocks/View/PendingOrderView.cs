namespace core.Stocks.View
{
    public class PendingOrderView
    {
        public PendingOrderView(int quantity, decimal price, string ticker, string type)
        {
            Quantity = quantity;
            Price = price;
            Ticker = ticker;
            Type = type;
        }

        public int Quantity { get; }
        public decimal Price { get; }
        public string Ticker { get; }
        public string Type { get; }
    }
}