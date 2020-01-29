using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionState
    {
        public OwnedOptionState()
        {
            this.Transactions = new List<Transaction>();
        }

        public Guid Id { get; private set; }
        public string Ticker { get; internal set; }
        public double StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; internal set; }
        public PositionType PositionType { get; private set; }
        public DateTimeOffset? Closed { get; internal set; }
        public DateTimeOffset? Filled { get; internal set; }
        public OptionType OptionType { get; internal set; }
        public string UserId { get; internal set; }
        public int NumberOfContracts { get; internal set; }
        public double Premium { get; internal set; }
        public double Spent { get; internal set; }
        public double CollateralCash => this.OptionType == OptionType.PUT ? StrikePrice * 100 - Premium : 0;
        public int CollateralShares => this.OptionType == OptionType.CALL ? 100 * NumberOfContracts : 0;

        public List<Transaction> Transactions { get; private set; }
        public bool IsOpen => this.Closed == null;

        internal void Apply(OptionSold sold)
        {
            this.Id = sold.AggregateId;
            this.Ticker = sold.Ticker;
            this.StrikePrice = sold.StrikePrice;
            this.Expiration = sold.Expiration;
            this.PositionType = PositionType.Sell;
            this.OptionType = sold.Type;
            this.UserId = sold.UserId;
            this.NumberOfContracts += sold.Amount;
            this.Premium += sold.Premium;

            // TODO: this does not make sense for multiple opens?
            // should each open stay separate and no ++ on amount/premium
            this.Filled = sold.When;

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Sold {sold.Amount} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contract(s) for ${this.Premium}",
                    0,
                    0,
                    sold.When
                )
            );
        }

        internal void Apply(OptionOpened sold)
        {
            this.Id = sold.AggregateId;
            this.Ticker = sold.Ticker;
            this.StrikePrice = sold.StrikePrice;
            this.Expiration = sold.Expiration;
            this.PositionType = sold.PositionType;
            this.OptionType = sold.OptionType;
            this.UserId = sold.UserId;
            this.NumberOfContracts += sold.NumberOfContracts;
            this.Premium += sold.Premium;

            // TODO: this does not make sense for multiple opens?
            // should each open stay separate and no ++ on amount/premium
            this.Filled = sold.When;

            var type = this.PositionType == PositionType.Sell ? "Sold" : "Bought";

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"{type} {sold.NumberOfContracts} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contract(s) for ${this.Premium}",
                    0,
                    0,
                    sold.When
                )
            );
        }

        internal void Apply(OptionClosed closed)
        {
            this.NumberOfContracts -= closed.Amount;
            this.Spent += closed.Money * closed.Amount;

            if (this.NumberOfContracts == 0)
            {
                this.Closed = closed.When;
            }

            this.Transactions.Add(
                new Transaction(
                    this.Ticker,
                    $"Closed {closed.Amount} ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contracts",
                    closed.Money * closed.Amount,
                    this.Premium * closed.Amount - closed.Money * closed.Amount,
                    closed.When
                )
            );
        }
    }
}