using System;

namespace core.Shared
{
    public struct Transaction
    {
        public Guid AggregateId { get; }
        public Guid EventId { get; }
        public string Ticker { get; }
        public string Description { get; }
        public double Debit { get; }
        public double Credit { get; }
        public DateTimeOffset Date { get; }
        public double Profit => this.Credit - this.Debit;
        public double ReturnPct
        {
            get
            {
                if (this.Debit > 0)
                {
                    return (this.Credit - this.Debit) * 1.0 / this.Debit;
                }

                return 0;
            }
        }

        public bool IsOption { get; }
        public bool IsPL { get; }

        private Transaction(
            Guid aggregateId,
            Guid eventId,
            string ticker,
            string description,
            double debit,
            double credit,
            DateTimeOffset when,
            bool isOption,
            bool isPL)
        {
            this.AggregateId = aggregateId;
            this.EventId = eventId;
            this.Ticker = ticker;
            this.Description = description;
            this.Debit = debit;
            this.Credit = credit;
            this.Date = when;
            this.IsOption = isOption;
            this.IsPL = isPL;
        }

        public static Transaction CreditTx(Guid aggregateId, Guid eventId, string ticker, string description, double credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, 0, credit, when, isOption, false);
        }

        public static Transaction DebitTx(Guid aggregateId, Guid eventId, string ticker, string description, double debit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, debit, 0, when, isOption, false);
        }

        public static Transaction PLTx(Guid aggregateId, Guid eventId, string ticker, string description, double debit, double credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, debit, credit, when, isOption, true);
        }
    }
}