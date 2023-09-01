#nullable enable
using System;
using System.Linq;
using core.Options;

namespace core.Adapters.Options
{
    public class OptionChain
    {
        public OptionChain(string symbol, decimal volatility, int numberOfContracts, OptionDetail[] options)
        {
            Symbol = symbol;
            Volatility = volatility;
            NumberOfContracts = numberOfContracts;
            Options = options;
        }
        public string Symbol { get; }
        public decimal Volatility { get; }
        public int NumberOfContracts { get; }
        public OptionDetail[] Options { get;}

        public OptionDetail? FindMatchingOption(decimal strikePrice, string expirationDate, OptionType optionType)
        {
            return Options.FirstOrDefault(
                o => o.StrikePrice == strikePrice
                && o.ExpirationDate == expirationDate
                && o.OptionType == optionType.ToString().ToLowerInvariant());
        }
    }

    public class OptionDetail
    {
        public OptionDetail(string symbol, string side, string description)
        {
            Symbol = symbol;
            Side = side;
            Description = description;
        }

        public string Symbol { get; }
        public string Description { get; }
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
        public string? ExchangeName { get; set; }
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

                var diff = Math.Max((int)date.Subtract(today).TotalDays, 1);

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
    }
}

#nullable restore