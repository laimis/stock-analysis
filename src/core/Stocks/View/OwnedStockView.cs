using core.Portfolio.Output;

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
        }

        public void ApplyPrice(TickerPrice price)
        {
            Price = price.Amount;

            Equity = Owned * price.Amount;
        }

        public System.Guid Id { get; set; }
        public double Price { get; set; }
        public string Category { get; set; }
        public int DaysHeld { get; set; }
        public string Ticker { get; set; }
        public int Owned { get; set; }
        public double Equity { get; set; }
        public string Description { get; set; }
        public double AverageCost { get; set; }
        public double Cost { get; set; }
        public double Profits => Equity - Cost;
        public double ProfitsPct => Profits / (1.0 * Cost);
    }
}