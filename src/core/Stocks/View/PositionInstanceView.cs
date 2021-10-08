namespace core.Stocks.View
{
    public class PositionInstanceView
    {
        public  PositionInstanceView(){}
        public PositionInstanceView(PositionInstance t)
        {
            Ticker = t.Ticker;
            Date = t.Closed.Value.ToString("yyyy-MM-dd");
            Profit = t.Profit;
            ReturnPct = t.Percentage;
            DaysHeld = t.DaysHeld;
        }

        public string Ticker { get; set; }
        public string Date { get; set; }
        public decimal Profit { get; set; }
        public decimal ReturnPct { get; set; }
        public int DaysHeld { get; set; }
    }
}