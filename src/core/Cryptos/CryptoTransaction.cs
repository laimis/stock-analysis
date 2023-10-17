using System;
using core.Shared;

namespace core.Cryptos
{
    public class CryptoTransaction
    {
        public Guid AggregateId;
        public Guid TransactionId;
        public string Token;
        public string Description;
        public decimal Price;
        public decimal Debit;
        public decimal Credit;
        public DateTimeOffset When;

        public CryptoTransaction(Guid aggregateId, Guid transactionId, string token, string description, decimal price, decimal debit, decimal credit, DateTimeOffset when)
        {
            AggregateId = aggregateId;
            TransactionId = transactionId;
            Token = token;
            Description = description;
            Price = price;
            Debit = debit;
            Credit = credit;
            When = when;
        }

        internal static CryptoTransaction DebitTx(Guid aggregateId, Guid transactionId, string token, string description, decimal price, decimal dollarAmount, DateTimeOffset when)
        {
            return new CryptoTransaction(
                aggregateId,
                transactionId,
                token,
                description,
                price,
                debit: dollarAmount,
                credit: 0,
                when
            );
        }

        internal static CryptoTransaction CreditTx(Guid aggregateId, Guid transactionId, string token, string description, decimal price, decimal dollarAmount, DateTimeOffset when)
        {
            return new CryptoTransaction(
                aggregateId,
                transactionId,
                token,
                description,
                price: price,
                debit: 0,
                credit: dollarAmount,
                when
            );
        }

        public Transaction ToSharedTransaction() => Credit switch {
            > 0 => Transaction.NonPLTx(AggregateId, TransactionId, new Ticker(Token), Description, Price, Credit, When, isOption: false),
            _ => Transaction.NonPLTx(AggregateId, TransactionId, new Ticker(Token), Description, Price, Debit, When, isOption: false)
        };
    }
}