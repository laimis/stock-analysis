using System;

namespace core.Portfolio
{
    public class OwnedOptionState
    {
        public string Ticker { get; internal set; }
        public double StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; internal set; }
        public OptionType Type { get; internal set; }
        public string UserId { get; internal set; }
        public string Key => OwnedOption.GenerateKey(this.Ticker, this.Type, this.Expiration, this.StrikePrice);

        public int Amount { get; internal set; }
        public DateTime Filled { get; internal set; }
        public double Premium { get; internal set; }
    }
}