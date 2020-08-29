using core.Portfolio.Output;

namespace core.Stocks.View
{
    public class OwnedStockView
    {
        public OwnedStockView(OwnedStock o, TickerPrice price)
        {
            var equity = o.State.Owned * price.Amount;
            var cost = o.State.Cost;
            var profits = equity - cost;
            var profitsPct = profits / (1.0 * cost);

            Id = o.Id;
            CurrentPrice = price.Amount;
            Category = o.State.Category;
            Ticker = o.State.Ticker;
            Owned = o.State.Owned;
            Equity = equity;
            Description = o.State.Description;
            AverageCost = o.State.AverageCost;
            Cost = cost;
            Profits = profits;
            ProfitsPct = profitsPct;
            Transactions = new TransactionList(o.State.Transactions, null, null);
            DaysHeld = o.State.DaysHeld;
        }

        public System.Guid Id { get; }
        public double CurrentPrice { get; }
        public string Category { get; }
        public int DaysHeld { get; }
        public string Ticker { get; }
        public int Owned { get; }
        public double Equity { get; }
        public string Description { get; }
        public double AverageCost { get; }
        public double Cost { get; }
        public double Profits { get; }
        public double ProfitsPct { get; }
        public TransactionList Transactions { get; }
    }
}