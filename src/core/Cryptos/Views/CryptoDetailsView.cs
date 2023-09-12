using System.Collections.Generic;
using core.Shared;

namespace core.Cryptos.Views
{
    public class CryptoDetailsView
    {
        public CryptoDetailsView(string token, Price? price)
        {
            Token = token;
            Price = price;
        }

        public string Token { get; }
        public Price? Price { get; }
    }
}