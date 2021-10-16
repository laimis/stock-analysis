using System;

namespace core.Adapters.Options
{
    public class OptionDetail
    {
        public string Symbol { get; set; }

        public string Id { get; set; }

        public string ExpirationDate { get; set; }

        public long ContractSize { get; set; }

        public double StrikePrice { get; set; }

        public double ClosingPrice { get; set; }

        public string Side { get; set; }

        public long Volume { get; set; }

        public long OpenInterest { get; set; }

        public double Bid { get; set; }

        public double Ask { get; set; }

        public DateTimeOffset LastUpdated { get; set; }
        public string OptionType => Side;
        public bool IsCall => Side == "call";
        public bool IsPut => Side == "put";
        public double Spread => (Ask - Bid);

        public double PerDayPrice
        {
            get
            {
                var date = ParsedExpirationDate;

                var today = DateTime.UtcNow.Date;

                var diff = (int)date.Subtract(today).TotalDays;

                return Bid * 100 / diff;
            }
        }

        public double BreakEven
        {
            get
            {
                return IsCall ?
                    StrikePrice + Bid
                    : StrikePrice - Bid;
            }
        }

        public double Risk => Bid / StrikePrice;

        public DateTime ParsedExpirationDate => DateTime.ParseExact(ExpirationDate, "yyyyMMdd", null).Date;
    }
}