using System;

namespace core.Crypto
{
    public struct Token
    {
        private readonly string _symbol;

        public Token(string symbol)
        {
            if (string.IsNullOrWhiteSpace(symbol))
            {
                throw new ArgumentException(nameof(symbol), "Symbol cannot be blank");
            }
            _symbol = symbol.ToUpper();
        }

        public static implicit operator string(Token t) => t._symbol;
        public static implicit operator Token(string t) => new Token(t);

        public string Value => _symbol;
    }
}