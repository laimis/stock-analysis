using System;

namespace core.Shared
{
    public struct Ticker
    {
        private readonly string _ticker;

        public Ticker(string ticker)
        {
            if (string.IsNullOrEmpty(ticker))
            {
                throw new ArgumentException(nameof(ticker), "Ticker cannot be blank");
            }
            _ticker = ticker.ToUpper();
        }

        public static implicit operator string(Ticker t) => t._ticker;
        public static implicit operator Ticker(string t) => new Ticker(t);
    }
}