namespace core.Cryptos.Views
{
    public class OwnedCryptoView
    {
        public OwnedCryptoView(OwnedCrypto c)
        {
            Quantity = c.State.Quantity;
            Cost = c.State.Cost;
        }

        public double Quantity { get; }
        public double Cost { get; }
    }
}