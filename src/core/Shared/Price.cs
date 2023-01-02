namespace core
{
    public struct Price
    {
        public static readonly Price Failed = new Price(0);
        
        public decimal Amount { get; }

        public Price(decimal amount)
        {
            Amount = amount;
        }

        public bool NotFound => Amount == 0;
    }
}