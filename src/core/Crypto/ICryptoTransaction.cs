using System;

namespace core.Crypto
{
    public interface ICryptoTransaction
    {
        Guid Id { get; }
        DateTimeOffset When { get; }
        double Quantity { get; }
        double DollarAmount { get; }
    }
}