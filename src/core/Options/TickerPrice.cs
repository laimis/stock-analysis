namespace core.Options
{
    public struct TickerPrice
    {
        public double Amount { get; }

        public TickerPrice(double amount)
        {
            this.Amount = amount;
        }

        public bool NotFound => this.Amount == 0;
    }
}