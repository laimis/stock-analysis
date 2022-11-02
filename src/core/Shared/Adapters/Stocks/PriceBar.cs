using System;

namespace core.Shared.Adapters.Stocks
{
    public enum PriceFrequency
    {
        Daily,
        Weekly,
        Monthly
    }
    
    public struct PriceBar : IEquatable<PriceBar>
    {
        public string Date { get; set; }
        public DateTimeOffset DateParsed => DateTimeOffset.Parse(Date);
        public decimal Close { get; set; }
        public decimal High { get; set; }
        public decimal Low { get; set; }
        public decimal Open { get; set; }
        public decimal Volume { get; set; }

        public bool Equals(PriceBar other) => Date == other.Date;

        public override bool Equals(object obj) => obj is PriceBar other && Equals(other);

        public override int GetHashCode() => Date.GetHashCode();
    }
}