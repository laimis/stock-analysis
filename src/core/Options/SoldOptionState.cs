using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class SoldOptionState
    {
        public SoldOptionState()
        {
            this.Transactions = new List<Transaction>();
        }

        public string Ticker { get; internal set; }
        public double StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; internal set; }
        public DateTimeOffset? Closed { get; internal set; }
        public DateTimeOffset? Filled { get; internal set; }
        public OptionType Type { get; internal set; }
        public string UserId { get; internal set; }
        public string Key => SoldOption.GenerateKey(this.Ticker, this.Type, this.Expiration, this.StrikePrice);
        public int Amount { get; internal set; }
        public double Premium { get; internal set; }
        public double Spent { get; internal set; }
        public double Profit => this.Premium - this.Spent;
        public double CollateralCash => this.Type == OptionType.PUT ? StrikePrice * 100 - Premium : 0;
        public int CollateralShares => this.Type == OptionType.CALL ? 100 * Amount : 0;

        public List<Transaction> Transactions { get; private set; }

        internal void Apply(OptionOpened opened)
        {
            this.Amount += opened.Amount;
            this.Filled = opened.Filled;
            this.Premium += opened.Premium;

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Sold ${this.StrikePrice} {this.Type} {this.Expiration.ToString("MM/dd")} option",
                    opened.Amount * opened.Premium,
                    opened.When
                )
            );
        }

        internal void Apply(OptionClosed closed)
        {
            this.Amount -= closed.Amount;
            this.Spent += closed.Money;

            if (this.Amount == 0)
            {
                this.Closed = closed.When;
            }

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Closed ${this.StrikePrice} {this.Type} {this.Expiration.ToString("MM/dd")} option",
                    -1 * closed.Money * closed.Amount,
                    closed.When
                )
            );
        }
    }
}