using System;
using core.Adapters.Options;

namespace core.Options
{
    public class OptionDetailsViewModel
    {
        public decimal StockPrice { get; set; }
        public OptionDetail[] Options { get; set; }
        public string[] Expirations { get; set; }
        public OptionBreakdownViewModel Breakdown { get; set; }
        public DateTimeOffset LastUpdated { get; set; }
    }
}