namespace core.Cryptos.Views
{
    public class OwnedCryptoView
    {
        public OwnedCryptoView(OwnedCrypto c)
        {
            Token = c.State.Token;
            Quantity = c.State.Quantity;
            Cost = c.State.Cost;
            DaysHeld = c.State.DaysHeld;
            AverageCost = c.State.AverageCost;
        }

        public string Token { get; }
        public decimal Quantity { get; }
        public decimal Cost { get; }
        public int DaysHeld { get; }
        public decimal AverageCost { get; }
    }
}