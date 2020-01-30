﻿using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionState
    {
        public OwnedOptionState()
        {
            this.Transactions = new List<Transaction>();
            this.Buys = new List<OptionPurchased>();
            this.Sells = new List<OptionSold>();
            this.Expirations = new List<OptionExpired>();
        }

        public Guid Id { get; private set; }
        public string Ticker { get; internal set; }
        public double StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; private set; }
        public bool IsExpired => DateTimeOffset.UtcNow > this.Expiration;
        public OptionType OptionType { get; internal set; }
        public string UserId { get; internal set; }
        public int NumberOfContracts { get; internal set; }
        public double Credit { get; private set; }
        public double Debit { get; private set; }
        
        public List<Transaction> Transactions { get; private set; }
        public List<OptionSold> Sells { get; private set; }
        public List<OptionPurchased> Buys { get; private set; }
        public List<OptionExpired> Expirations { get; private set; }

        internal void Apply(OptionExpired expired)
        {
            this.NumberOfContracts = 0;

            this.Expirations.Add(expired);
        }

        internal void Apply(OptionSold sold)
        {
            this.NumberOfContracts -= sold.Amount;

            this.Sells.Add(sold);

            var credit = (sold.Amount * sold.Premium);

            this.Credit += credit;

            this.Transactions.Add(
                Transaction.CreditTx(
                    this.Ticker,
                    $"Sold {sold.Amount} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contract(s) for ${sold.Premium} premium/contract",
                    credit,
                    sold.When
                )
            );
        }

        internal void Apply(OptionOpened opened)
        {
            this.Id = opened.AggregateId;
            this.Ticker = opened.Ticker;
            this.StrikePrice = opened.StrikePrice;
            this.Expiration = opened.Expiration;
            this.OptionType = opened.OptionType;
            this.UserId = opened.UserId;
        }

        internal bool IsMatch(string ticker, double strike, OptionType type, DateTimeOffset expiration)
        {
            return this.Ticker == ticker 
                && this.StrikePrice == strike 
                && this.OptionType == type 
                && this.Expiration.Date == expiration.Date;
        }

        internal void Apply(OptionPurchased purchased)
        {
            this.NumberOfContracts += purchased.NumberOfContracts;

            var debit = purchased.NumberOfContracts * purchased.Premium;

            this.Debit += debit;

            this.Buys.Add(purchased);

            this.Transactions.Add(
                Transaction.DebitTx(
                    this.Ticker,
                    $"Bought {purchased.NumberOfContracts} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contracts for ${purchased.Premium}/contract",
                    debit,
                    purchased.When
                )
            );
        }
    }
}