namespace core.Cryptos.Views
{
    public class OwnedCryptoView
    {
        public OwnedCryptoView(OwnedCrypto c)
        {
            Token = c.State.Token;
            Quantity = c.State.Quantity;
            Cost = c.State.Cost;
        }

        public string Token { get; }
        public decimal Quantity { get; }
        public decimal Cost { get; }
    }
}