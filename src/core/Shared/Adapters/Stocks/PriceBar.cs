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
    }

    public static class PriceBarExtensions
    {
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