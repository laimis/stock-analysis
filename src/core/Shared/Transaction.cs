using System;

namespace core.Shared
{
    public struct Transaction
    {
        public string Ticker { get; }
        public string Description { get; }
        public double Debit { get; }
        public double Credit { get; }
        public DateTimeOffset Date { get; }

        public Transaction(string ticker, string description, double debit, double credit, DateTimeOffset when)
        {
            this.Ticker = ticker;
            this.Description = description;
            this.Debit = debit;
            this.Credit = credit;
            this.Date = when;
        }
    }
}