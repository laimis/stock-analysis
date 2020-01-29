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

        public Guid Id { get; private set; }
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
        public double CollateralCash => this.Type == OptionType.PUT ? StrikePrice * 100 - Premium : 0;
        public int CollateralShares => this.Type == OptionType.CALL ? 100 * Amount : 0;

        public List<Transaction> Transactions { get; private set; }
        public bool IsOpen => this.Closed == null;

        internal void Apply(OptionSold sold)
        {
            this.Id = sold.AggregateId;
            this.Ticker = sold.Ticker;
            this.StrikePrice = sold.StrikePrice;
            this.Expiration = sold.Expiration;
            this.Type = sold.Type;
            this.UserId = sold.UserId;
            this.Amount += sold.Amount;
            this.Premium += sold.Premium;

            // TODO: this does not make sense for multiple opens?
            // should each open stay separate and no ++ on amount/premium
            this.Filled = sold.When;

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Sold {sold.Amount} x ${this.StrikePrice} {this.Type} {this.Expiration.ToString("MM/dd")} contract(s) for ${this.Premium}",
                    0,
                    0,
                    sold.When
                )
            );
        }

        internal void Apply(OptionClosed closed)
        {
            this.Amount -= closed.Amount;
            this.Spent += closed.Money * closed.Amount;

            if (this.Amount == 0)
            {
                this.Closed = closed.When;
            }

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Closed {closed.Amount} ${this.StrikePrice} {this.Type} {this.Expiration.ToString("MM/dd")} contracts",
                    closed.Money * closed.Amount,
                    this.Premium * closed.Amount - closed.Money * closed.Amount,
                    closed.When
                )
            );
        }
    }
}