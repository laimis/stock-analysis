namespace core
{
    public struct Price
    {
        public decimal Amount { get; }

        public Price(decimal amount)
        {
            Amount = amount;
        }

        public bool NotFound => Amount == 0;
    }
}