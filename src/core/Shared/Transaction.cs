using System;

namespace core.Shared
{
    public struct Transaction
    {
        public string Ticker { get; }
        public string Description { get; }
        public double Value { get; }
        public double Profit { get; }
        public DateTimeOffset Date { get; }

        public Transaction(string ticker, string description, double value, double profit, DateTimeOffset when)
        {
            this.Ticker = ticker;
            this.Description = description;
            this.Value = value;
            this.Profit = profit;
            this.Date = when;
        }
    }
}