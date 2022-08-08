namespace core.Stocks.View
{
    public class BrokerageOrderView
    {
        public BrokerageOrderView(string orderId, int quantity, decimal price, string status, string ticker, string type)
        {
            OrderId = orderId;
            Price = price;
            Quantity = quantity;
            Status = status;
            Ticker = ticker;
            Type = type;
        }

        public string OrderId { get; }
        public int Quantity { get; }
        public decimal Price { get; }
        public string Status { get; }
        public string Ticker { get; }
        public string Type { get; }
    }
}