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
        public double Profit => this.Credit - this.Debit;

        private Transaction(string ticker, string description, double debit, double credit, DateTimeOffset when)
        {
            this.Ticker = ticker;
            this.Description = description;
            this.Debit = debit;
            this.Credit = credit;
            this.Date = when;
        }

        public static Transaction CreditTx(string ticker, string description, double amount, DateTimeOffset when)
        {
            return new Transaction(ticker, description, 0, amount, when);
        }

        public static Transaction DebitTx(string ticker, string description, double amount, DateTimeOffset when)
        {
            return new Transaction(ticker, description, amount, 0, when);
        }
    }
}