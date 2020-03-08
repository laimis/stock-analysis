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
        public bool IsOption { get; }
        public bool IsPL { get; }

        private Transaction(
            string ticker,
            string description,
            double debit,
            double credit,
            DateTimeOffset when,
            bool isOption,
            bool isPL)
        {
            this.Ticker = ticker;
            this.Description = description;
            this.Debit = debit;
            this.Credit = credit;
            this.Date = when;
            this.IsOption = isOption;
            this.IsPL = isPL;
        }

        public static Transaction CreditTx(string ticker, string description, double credit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(ticker, description, 0, credit, when, isOption, false);
        }

        public static Transaction DebitTx(string ticker, string description, double debit, DateTimeOffset when, bool isOption)
        {
            return new Transaction(ticker, description, debit, 0, when, isOption, false);
        }

        public static Transaction PLTx(string ticker, string description, double amount, DateTimeOffset when, bool isOption)
        {
            return new Transaction(ticker, description, amount < 0 ? Math.Abs(amount) : 0, amount > 0 ? amount : 0, when, isOption, true);
        }
    }
}