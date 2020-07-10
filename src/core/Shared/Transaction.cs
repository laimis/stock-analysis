using System;

namespace core.Shared
{
    public struct Transaction
    {
        public Guid AggregateId { get; }
        public string Ticker { get; }
        public string Description { get; }
        public double Debit { get; }
        public double Credit { get; }
        public DateTimeOffset Date { get; }
        public double Profit => this.Credit - this.Debit;
        public bool IsOption { get; }
        public bool IsPL { get; }

        private Transaction(
            Guid aggregateId,
            string ticker,
            string description,
            double debit,
            double credit,
            DateTimeOffset when,
            bool isOption,
            bool isPL)
        {
            this.AggregateId = aggregateId;
            this.Ticker = ticker;
            this.Description = description;
            this.Debit = debit;
            this.Credit = credit;
            this.Date = when;
            this.IsOption = isOption;
            this.IsPL = isPL;
        }

        public static Transaction CreditTx(Guid aggregateId, string ticker, string description, double credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, ticker, description, 0, credit, when, isOption, false);
        }

        public static Transaction DebitTx(Guid aggregateId, string ticker, string description, double debit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, ticker, description, debit, 0, when, isOption, false);
        }

        public static Transaction PLTx(Guid aggregateId, string ticker, string description, double amount, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, ticker, description, amount < 0 ? Math.Abs(amount) : 0, amount > 0 ? amount : 0, when, isOption, true);
        }
    }
}