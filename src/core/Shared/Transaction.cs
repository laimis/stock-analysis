using System;

namespace core.Shared
{
    public struct Transaction
    {
        public string Ticker { get; }
        public string Description { get; }
        public double Value { get; }
        public DateTime Date { get; }

        public Transaction(string ticker, string description, double value, DateTime when)
        {
            this.Ticker = ticker;
            this.Description = description;
            this.Value = value;
            this.Date = when;
        }
    }
}