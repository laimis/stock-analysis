using System;

namespace web
{
    public class OpenModel
    {
        public string Ticker { get; set; }
        public float StrikePrice { get; set; }
        public DateTimeOffset ExpirationDate { get; set; }
        public string OptionType { get; set; }
        public int Amount { get; set; }
        public float Bid { get; set; }
        public DateTimeOffset Filled { get; set; }
    }
}