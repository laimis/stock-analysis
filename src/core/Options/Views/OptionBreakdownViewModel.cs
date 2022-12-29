namespace core.Options
{
    public class OptionBreakdownViewModel
    {
        public long PutVolume { get; set; }
        public long CallVolume { get; set; }
        public decimal PutSpend { get; set; }
        public decimal CallSpend { get; set; }
        public decimal PriceBasedOnCalls { get; set; }
        public decimal PriceBasedOnPuts { get; set; }
    }
}