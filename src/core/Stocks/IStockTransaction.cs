namespace core.Stocks
{
    internal interface IStockTransaction
    {
        int NumberOfShares { get; }
        double Price { get; }
    }
}