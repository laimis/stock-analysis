using System;

namespace core.Cryptos
{
    public interface ICryptoTransaction
    {
        Guid Id { get; }
        DateTimeOffset When { get; }
        decimal Quantity { get; }
        decimal DollarAmount { get; }
    }
}