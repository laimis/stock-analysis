using core.Options;

namespace web.Models
{
    public class OptionDetailsViewModel
    {
        public double StockPrice { get; set; }
        public OptionDetail[] Options { get; set; }
        public string[] Expirations { get; set; }
        public OptionBreakdownViewModel Breakdown { get; set; }
    }
}