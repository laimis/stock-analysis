namespace core.Stocks.View
{
    public class BrokerageOrderView
    {
        public BrokerageOrderView(bool canBeCancelled, bool isActive, string orderId, int quantity, decimal price, string status, string ticker, string type)
        {
            CanBeCancelled = canBeCancelled;
            IsActive = isActive;
            OrderId = orderId;
            Price = price;
            Quantity = quantity;
            Status = status;
            Ticker = ticker;
            Type = type;
        }

        public bool CanBeCancelled { get; }
        public bool IsActive { get; }
        public string OrderId { get; }
        public int Quantity { get; }
        public decimal Price { get; }
        public string Status { get; }
        public string Ticker { get; }
        public string Type { get; }
    }
}