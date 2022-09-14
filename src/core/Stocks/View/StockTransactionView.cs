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
            Amount = t.Amount;
        }

        public string Ticker { get; set; }
        public string Date { get; set; }
        public decimal Amount { get; set; }
    }
}