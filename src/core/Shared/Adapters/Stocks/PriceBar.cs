using System;
using System.Collections.Generic;
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