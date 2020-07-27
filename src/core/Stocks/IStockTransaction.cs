using System;

namespace core.Stocks
{
    internal interface IStockTransaction
    {
        int NumberOfShares { get; }
        double Price { get; }
        Guid Id { get; }
        DateTimeOffset When { get; }
    }
}