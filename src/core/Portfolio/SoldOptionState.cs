using System;

namespace core.Portfolio
{
    public class SoldOptionState
    {
        public string Ticker { get; internal set; }
        public double StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; internal set; }
        public OptionType Type { get; internal set; }
        public string UserId { get; internal set; }
        public string Key => SoldOption.GenerateKey(this.Ticker, this.Type, this.Expiration, this.StrikePrice);
        public int Amount { get; internal set; }
        public double Premium { get; internal set; }
        public double Spent { get; internal set; }
        public double Profit => this.Premium - this.Spent;
        public double CollateralCash => this.Type == OptionType.PUT ? StrikePrice * 100 - Premium : 0;
        public int CollateralShares => this.Type == OptionType.CALL ? 100 * Amount : 0;
    }
}