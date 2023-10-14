using System;

namespace core.Shared
{
    public struct Ticker : IComparable
    {
        private readonly string _ticker;

        public Ticker(string ticker)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new ArgumentException(nameof(ticker), "Ticker cannot be blank");
            }
            _ticker = ticker.ToUpper();
        }

        public static implicit operator string(Ticker t) => t._ticker;
        public static implicit operator Ticker(string t) => new Ticker(t);

        public string Value => _ticker;
        public int CompareTo(object obj)
        {
            if (obj is Ticker t)
            {
                return string.CompareOrdinal(_ticker, t._ticker);
            }

            return -1;
        }
    }
}