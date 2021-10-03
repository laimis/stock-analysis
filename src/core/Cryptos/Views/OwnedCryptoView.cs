using System;

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
        public decimal Price { get; private set; }
        public decimal Equity => Price * Quantity;
        public decimal Profits => Equity - Cost;
        public decimal ProfitsPct => Cost switch {
            0 => 1m,
            _ => Profits / (1.0m * Cost)
        };

        internal void ApplyPrice(Price price)
        {
            Price = Convert.ToDecimal(price.Amount);
        }
    }
}