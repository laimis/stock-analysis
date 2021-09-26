namespace core
{
    public struct Price
    {
        public double Amount { get; }

        public Price(double amount)
        {
            this.Amount = amount;
        }

        public bool NotFound => this.Amount == 0;
    }
}