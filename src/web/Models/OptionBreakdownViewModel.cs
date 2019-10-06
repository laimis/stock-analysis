namespace web.Models
{
    public class OptionBreakdownViewModel
    {
        public long PutVolume { get; set; }
        public long CallVolume { get; set; }
        public double PutSpend { get; set; }
        public double CallSpend { get; set; }
        public double PriceBasedOnCalls { get; set; }
        public double PriceBasedOnPuts { get; set; }
    }
}