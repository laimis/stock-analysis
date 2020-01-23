namespace core.Adapters.Stocks
{
    public class StockAdvancedStats
    {
        public double Week52Change { get; set; }
        public double Week52High { get; set; }
        public double Week52Low { get; set; }
        public double MarketCap { get; set; }
        public int? Employees { get; set; }
        public double Day200MovingAvg { get; set; }
        public double Day50MovingAvg { get; set; }
        public double? Float { get; set; }
        public double Avg10Volume { get; set; }
        public double Avg30Volume { get; set; }
        public double? TTMEPS { get; set; }
        public double? TTMDividendRate { get; set; }
        public string CompanyName { get; set; }
        public long SharesOutstanding { get; set; }
        public double MaxChangePercent { get; set; }
        public double Year5ChangePercent { get; set; }
        public double Year2ChangePercent { get; set; }
        public double Year1ChangePercent { get; set; }
        public double YTDChangePercent { get; set; }
        public double Month6ChangePercent { get; set; }
        public double Month3ChangePercent { get; set; }
        public double Month1ChangePercent { get; set; }
        public double Day30ChangePercent { get; set; }
        public double Day5ChangePercent { get; set; }
        public string nextDividendDate { get; set; }
        public double? DividendYield { get; set; }
        public string NextEarningsDate { get; set; }
        public string ExDividendDate { get; set; }
        public double PERatio { get; set; }
        public double Beta { get; set; }
        public long? TotalCash { get; set; }
        public long? CurrentDebt { get; set; }
        public long? Revenue { get; set; }
        public long? GrossProfit { get; set; }
        public long? TotalRevenue { get; set; }
        public long? EBITDA  { get; set; }
        public double? RevenuePerShare { get; set; }
        public double? RevenuePerEmployee { get; set; }
        public double? DebtToEquity { get; set; }
        public double? ProfitMargin { get; set; }
        public double? EnterpriseValue { get; set; }
        public double? EnterpriseValueToRevenue { get; set; }
        public double? PriceToSales { get; set; }
        public double? PriceToBook { get; set; }
        public double? ForwardPERatio { get; set; }
        public double? PEGRatio { get; set; }
        public double? PEHigh { get; set; }
        public double? PELow { get; set; }
        public string Week52highDate { get; set; }
        public string Week52lowDate { get; set; }
        public double? PutCallRatio { get; set; }
    }
}