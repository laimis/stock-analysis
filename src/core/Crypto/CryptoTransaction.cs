using System;

namespace core.Crypto
{
    public class CryptoTransaction
    {
        private Guid aggregateId;
        private Guid transactionId;
        private string token;
        private string description;
        private double debit;
        private double credit;
        private DateTimeOffset when;

        public CryptoTransaction(Guid aggregateId, Guid transactionId, string token, string description, double debit, double credit, DateTimeOffset when)
        {
            this.aggregateId = aggregateId;
            this.transactionId = transactionId;
            this.token = token;
            this.description = description;
            this.debit = debit;
            this.credit = credit;
            this.when = when;
        }

        internal static CryptoTransaction DebitTx(Guid aggregateId, Guid transactionId, string token, string description, double dollarAmount, DateTimeOffset when)
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

        internal static CryptoTransaction CreditTx(Guid aggregateId, Guid transactionId, string token, string description, double dollarAmount, DateTimeOffset when)
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
    }
}