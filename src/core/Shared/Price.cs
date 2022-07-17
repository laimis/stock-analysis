namespace core
{
    public struct Price
    {
        public static readonly Price Zero = new Price(0);
        
        public decimal Amount { get; }

        public Price(decimal amount)
        {
            Amount = amount;
        }

        public bool NotFound => Amount == 0;
    }
}