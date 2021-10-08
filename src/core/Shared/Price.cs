namespace core
{
    public struct Price
    {
        public decimal Amount { get; }

        public Price(decimal amount)
        {
            this.Amount = amount;
        }

        public bool NotFound => this.Amount == 0;
    }
}