using System;

namespace core.Shared
{
    public struct Ticker : IComparable, IEquatable<Ticker>
    {
        private readonly string _ticker;

        public Ticker(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(nameof(value), "Ticker cannot be blank");
            }
            _ticker = value.ToUpper();
        }

        public string Value => _ticker;
        public int CompareTo(object obj)
        {
            if (obj is Ticker t)
            {
                return string.CompareOrdinal(_ticker, t._ticker);
            }

            return -1;
        }

        public override string ToString() => _ticker;

        public bool Equals(Ticker other)
        {
            return _ticker == other._ticker;
        }

        public override bool Equals(object obj)
        {
            return obj is Ticker other && Equals(other);
        }

        public override int GetHashCode()
        {
            return (_ticker != null ? _ticker.GetHashCode() : 0);
        }
    }
}