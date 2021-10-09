using System.Collections.Generic;

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