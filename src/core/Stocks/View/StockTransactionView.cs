using core.Shared;

namespace core.Stocks.View
{
    public class StockTransactionView
    {
        public  StockTransactionView(){}
        public StockTransactionView(Transaction t)
        {
            Ticker = t.Ticker;
            Date = t.Date;
            Profit = t.Profit;
            ReturnPct = t.ReturnPct;
        }

        public string Ticker { get; set; }
        public string Date { get; set; }
        public decimal Profit { get; set; }
        public decimal ReturnPct { get; set; }
    }
}