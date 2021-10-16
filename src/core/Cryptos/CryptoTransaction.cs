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
        public decimal Debit;
        public decimal Credit;
        public DateTimeOffset When;

        public CryptoTransaction(Guid aggregateId, Guid transactionId, string token, string description, decimal debit, decimal credit, DateTimeOffset when)
        {
            AggregateId = aggregateId;
            TransactionId = transactionId;
            Token = token;
            Description = description;
            Debit = debit;
            Credit = credit;
            When = when;
        }

        internal static CryptoTransaction DebitTx(Guid aggregateId, Guid transactionId, string token, string description, decimal dollarAmount, DateTimeOffset when)
        {
            return new CryptoTransaction(
                aggregateId,
                transactionId,
                token,
                description,
                debit: dollarAmount,
                credit: 0,
                when
            );
        }

        internal static CryptoTransaction CreditTx(Guid aggregateId, Guid transactionId, string token, string description, decimal dollarAmount, DateTimeOffset when)
        {
            return new CryptoTransaction(
                aggregateId,
                transactionId,
                token,
                description,
                debit: 0,
                credit: dollarAmount,
                when
            );
        }

        internal Transaction ToSharedTransaction() => Credit switch {
            > 0 => Transaction.CreditTx(AggregateId, TransactionId, Token, Description, Credit, When, isOption: false),
            _ => Transaction.DebitTx(AggregateId, TransactionId, Token, Description, Debit, When, isOption: false)
        };
    }
}