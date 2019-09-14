using System;

namespace web
{
    public class OpenModel
    {
        public string Ticker { get; set; }
        public float StrikePrice { get; set; }
        public DateTimeOffset Expiration { get; set; }
        public OptionType OptionType { get; set; }
        public int Amount { get; set; }
        public float Premium { get; set; }
        public DateTimeOffset Filled { get; set; }
    }
}