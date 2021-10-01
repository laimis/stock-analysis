using System;
using core.Shared;

namespace core.Cryptos
{
    public class CryptoTransaction
    {
        private Guid aggregateId;
        private Guid transactionId;
        private string token;
        private string description;
        private decimal debit;
        private decimal credit;
        private DateTimeOffset when;

        public CryptoTransaction(Guid aggregateId, Guid transactionId, string token, string description, decimal debit, decimal credit, DateTimeOffset when)
        {
            this.aggregateId = aggregateId;
            this.transactionId = transactionId;
            this.token = token;
            this.description = description;
            this.debit = debit;
            this.credit = credit;
            this.when = when;
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

        internal Transaction ToSharedTransaction() => credit switch {
            > 0 => Transaction.CreditTx(aggregateId, transactionId, token, description, Decimal.ToDouble(credit), when, isOption: false),
            _ => Transaction.DebitTx(aggregateId, transactionId, token, description, Decimal.ToDouble(debit), when, isOption: false)
        };
    }
}