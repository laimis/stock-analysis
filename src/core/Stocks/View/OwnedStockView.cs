namespace core.Stocks.View
{
    public class OwnedStockView
    {
        public OwnedStockView() {}

        public OwnedStockView(OwnedStock o)
        {
            Id = o.Id;
            Category = o.State.Category;
            Ticker = o.State.Ticker;
            Owned = o.State.Owned;
            
            Description = o.State.Description;
            AverageCost = o.State.AverageCost;
            Cost = o.State.Cost;
            DaysHeld = o.State.DaysHeld;
            DaysSinceLastTransaction = o.State.DaysSinceLastTransaction;
        }

        public void ApplyPrice(decimal price)
        {
            Price = price;
            Equity = Owned * price;
        }

        public System.Guid Id { get; set; }
        public decimal Price { get; set; }
        public string Category { get; set; }
        public int DaysHeld { get; set; }
        public int DaysSinceLastTransaction { get; set; }
        public string Ticker { get; set; }
        public decimal Owned { get; set; }
        public decimal Equity { get; set; }
        public string Description { get; set; }
        public decimal AverageCost { get; set; }
        public decimal Cost { get; set; }
        public decimal Profits => Equity - Cost;
        public decimal ProfitsPct => Profits / (1.0m * Cost);
    }
}