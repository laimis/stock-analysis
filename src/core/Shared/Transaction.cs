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
        public decimal Amount { get; }
        public DateTimeOffset DateAsDate { get; }
        public string Date => DateAsDate.ToString("yyyy-MM-dd");
        public bool IsOption { get; }
        public bool IsPL { get; }

        private Transaction(
            Guid aggregateId,
            Guid eventId,
            string ticker,
            string description,
            decimal price,
            decimal amount,
            DateTimeOffset when,
            bool isOption,
            bool isPL)
        {
            AggregateId = aggregateId;
            EventId = eventId;
            Ticker = ticker;
            Description = description;
            Price = price;
            Amount = amount;
            DateAsDate = when;
            IsOption = isOption;
            IsPL = isPL;
        }

        public static Transaction NonPLTx(Guid aggregateId, Guid eventId, string ticker, string description, decimal price, decimal amount, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, eventId, ticker, description, price, amount, when, isOption, false);
        }

        public static Transaction PLTx(Guid aggregateId, string ticker, string description, decimal price, decimal amount, DateTimeOffset when, bool isOption)
        {
            return new Transaction(aggregateId, Guid.Empty, ticker, description, price, amount, when, isOption, true);
        }
    }
}