using System;

namespace core.Adapters.Options
{
    public class OptionChain
    {
        public string Symbol { get; set; }
        public decimal Volatility { get; set; }
        public int NumberOfContracts { get; set; }
        public OptionDetail[] Options { get; set; }
    }

    public class OptionDetail
    {
        public string Symbol { get; set; }
        public string ExpirationDate => ParsedExpirationDate.ToString("yyyy-MM-dd");
        public decimal StrikePrice { get; set; }
        public string Side { get; set; }
        public long Volume { get; set; }
        public long OpenInterest { get; set; }
        public decimal Bid { get; set; }
        public decimal Ask { get; set; }
        public string OptionType => Side;
        public bool IsCall => Side == "call";
        public bool IsPut => Side == "put";
        public decimal Spread => Ask - Bid;
        public decimal Volatility { get; set; }
        public decimal Delta { get; set; }
        public decimal Gamma { get; set; }
        public decimal Theta { get; set; }
        public decimal Vega { get; set; }
        public decimal Rho { get; set; }
        public decimal TimeValue { get; set; }
        public decimal IntrinsicValue { get; set; }
        public bool InTheMoney { get; set; }
        public string ExchangeName { get; set; }
        public int DaysToExpiration { get; set; }
        public decimal MarkChange { get; set; }
        public decimal MarkPercentChange { get; set; }
        public decimal? UnderlyingPrice { get; set; }

        public decimal PerDayPrice
        {
            get
            {
                var date = ParsedExpirationDate;

                var today = DateTime.UtcNow.Date;

                var diff = (int)date.Subtract(today).TotalDays;

                return Bid * 100 / diff;
            }
        }

        public decimal BreakEven
        {
            get
            {
                return IsCall ?
                    StrikePrice + Bid
                    : StrikePrice - Bid;
            }
        }

        public decimal Risk => Bid / StrikePrice;

        public DateTimeOffset ParsedExpirationDate { get; set; }

        public string Description { get; set; }
    }
}