using System;
using System.Linq;

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
        public PriceBar(DateTimeOffset date, decimal open, decimal high, decimal low, decimal close, long volume)
        {
            Date = date;
            DateStr = date.ToString("yyyy-MM-dd");
            Open = open;
            High = high;
            Low = low;
            Close = close;
            Volume = volume;
        }

        public string DateStr { get; }
        public DateTimeOffset Date { get; }
        public decimal Close { get; }
        public decimal High { get; }
        public decimal Low { get; }
        public decimal Open { get; }
        public decimal Volume { get; }

        public bool Equals(PriceBar other) => DateStr == other.DateStr;

        public override bool Equals(object obj) => obj is PriceBar other && Equals(other);

        public override int GetHashCode() => Date.GetHashCode();

        public override string ToString()
        {
            return $"{DateStr},{Open},{High},{Low},{Close},{Volume}";
        }
        public static PriceBar Parse(string value)
        {
            var parts = value.Split(',');
            return new PriceBar(
                DateTimeOffset.Parse(parts[0]),
                decimal.Parse(parts[1]),
                decimal.Parse(parts[2]),
                decimal.Parse(parts[3]),
                decimal.Parse(parts[4]),
                long.Parse(parts[5])
            );
        }

        internal decimal PercentDifferenceFromLow(decimal value) =>
            (Low - value) / value;

        internal decimal PercentDifferenceFromHigh(decimal value) =>
            (High - value) / value;
    }

    public static class PriceBarExtensions
    {
        public static decimal ClosingRange(this PriceBar currentBar)
        {
            var rangeDenominator = currentBar.High - currentBar.Low;
            return rangeDenominator switch {
                0 => 0,
                _ => (currentBar.Close - currentBar.Low) / rangeDenominator
            };
        }

        public static PriceBar[] Last(this PriceBar[] source, int numberOfItems)
        {
            return source.Skip(source.Length - numberOfItems).ToArray();
        }

        public static TOutput[] Select<TOutput>(this Span<PriceBar> source, Func<PriceBar, TOutput> selector)
        {
            var output = new TOutput[source.Length];
            for (var i = 0; i < source.Length; i++)
            {
                output[i] = selector(source[i]);
            }

            return output;
        }
    }
}