using System;

namespace core.Shared
{
    public struct Transaction
    {
        public Guid AggregateId { get; }
        public Guid EventId { get; }
        public string Ticker { get; }
        public string Description { get; }
        public decimal Price { get; set; }
        public decimal Debit { get; }
        public decimal Credit { get; }
        public DateTimeOffset DateAsDate { get; }
        public string Date => DateAsDate.ToString("yyyy-MM-dd");
        public decimal Profit => Credit - Debit;
        public decimal ReturnPct
        {
            get
            {
                if (Debit > 0)
                {
                    return (Credit - Debit) * 1.0m / Debit;
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
            decimal price,
            decimal debit,
            decimal credit,
            DateTimeOffset when,
            bool isOption,
            bool isPL)
        {
            AggregateId = aggregateId;
            EventId = eventId;
            Ticker = ticker;
            Description = description;
            Price = price;
            Debit = debit;
            Credit = credit;
            DateAsDate = when;
            IsOption = isOption;
            IsPL = isPL;
        }

        public static Transaction CreditTx(Guid aggregateId, Guid eventId, string ticker, string description, decimal price, decimal credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, price, 0, credit, when, isOption, false);
        }

        public static Transaction DebitTx(Guid aggregateId, Guid eventId, string ticker, string description, decimal price, decimal debit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, price, debit, 0, when, isOption, false);
        }

        public static Transaction PLTx(Guid aggregateId, string ticker, string description, decimal price, decimal debit, decimal credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, Guid.Empty, ticker, description, price, debit, credit, when, isOption, true);
        }
    }
}