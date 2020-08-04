using core.Shared;

namespace core.Stocks.View
{
    public class StockTransactionView
    {
        public StockTransactionView(Transaction t)
        {
            this.Ticker = t.Ticker;
            this.Date = t.Date;
            this.Profit = t.Profit;
            this.ReturnPct = t.ReturnPct;
        }

        public string Ticker { get; }
        public string Date { get; }
        public double Profit { get; }
        public double ReturnPct { get; }
    }
}