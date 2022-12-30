using System;
using core.Adapters.Options;

namespace core.Options
{
    public class OptionDetailsViewModel
    {
        public decimal? StockPrice { get; set; }
        public OptionDetail[] Options { get; set; }
        public string[] Expirations { get; set; }
        public decimal Volatility { get; set; }
        public decimal NumberOfContracts { get; set; }
    }
}