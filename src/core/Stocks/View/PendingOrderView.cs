namespace core.Stocks.View
{
    public class PendingOrderView
    {
        public PendingOrderView(string orderId, int quantity, decimal price, string ticker, string type)
        {
            OrderId = orderId;
            Price = price;
            Quantity = quantity;
            Ticker = ticker;
            Type = type;
        }

        public int Quantity { get; }
        public string OrderId { get; }
        public decimal Price { get; }
        public string Ticker { get; }
        public string Type { get; }
    }
}