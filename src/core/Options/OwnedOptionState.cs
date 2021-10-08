using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionState : IAggregateState
    {
        public OwnedOptionState()
        {
            this.Transactions = new List<Transaction>();
            this.Buys = new List<OptionPurchased>();
            this.Sells = new List<OptionSold>();
            this.Expirations = new List<OptionExpired>();
            this.Notes = new List<string>();
        }

        public Guid Id { get; private set; }
        public string Ticker { get; internal set; }
        public decimal StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; private set; }
        public bool IsExpired => DateTimeOffset.UtcNow.Date > this.Expiration;
        public OptionType OptionType { get; internal set; }
        public Guid UserId { get; internal set; }
        public int NumberOfContracts { get; internal set; }
        
        public List<Transaction> Transactions { get; private set; }
        public List<OptionSold> Sells { get; private set; }
        public List<OptionPurchased> Buys { get; private set; }
        public List<OptionExpired> Expirations { get; private set; }
        public double Days { get; private set; }
        public bool? SoldToOpen { get; private set; }
        public DateTimeOffset? FirstFill { get; private set; }

        public long DaysUntilExpiration => this.IsExpired ? 0 :
            (long)Math.Ceiling(Math.Abs(this.Expiration.Subtract(DateTimeOffset.UtcNow).TotalDays));

        public DateTimeOffset? Closed { get; private set; }
        public int DaysHeld
        {
            get
            {
                DateTimeOffset date = DateTimeOffset.UtcNow;
                if (this.Closed != null)
                {
                    date = this.Closed.Value;
                }

                if (this.IsExpired)
                {
                    date = this.Expiration;
                }

                var val = (int)Math.Floor(date.Subtract(this.FirstFill.Value).TotalDays);
                if (val == 0)
                {
                    val = 1;
                }
                return val;
            }
        }
        
        public bool Assigned => this.Expirations.Count > 0 && this.Expirations[0].Assigned;
        public bool Active => !this.Deleted && !this.IsExpired && this.NumberOfContracts != 0;
        public bool ExpiresSoon => !IsExpired && DaysUntilExpiration >= 0 && DaysUntilExpiration < 7;
        public long? DaysLeft => this.DaysUntilExpiration;
        public bool Deleted { get; private set; }
        private decimal PremiumReceived { get; set; }
        private decimal PremiumPaid { get; set; }
        public List<string> Notes { get; }

        internal void ApplyInternal(OptionDeleted deleted)
        {
            this.NumberOfContracts = 0;
            this.Transactions.Clear();
            this.Buys.Clear();
            this.Sells.Clear();
            this.FirstFill = null;
            this.SoldToOpen = null;
            this.Closed = null;
            this.Notes.Clear();
            this.Expirations.Clear();

            this.Deleted = true;
        }

        internal void ApplyInternal(OptionSold sold)
        {
            if (this.SoldToOpen == null)
            {
                ApplyFirstTransactionLogic(true, sold.When);
            }

            if (this.Deleted == true)
            {
                this.Deleted = false;
            }

            AddNoteIfNotEmpty(sold.Notes);

            if (this.NumberOfContracts == 0)
            {
                this.PremiumReceived = 0;
                this.PremiumPaid = 0;
            }

            this.NumberOfContracts -= sold.NumberOfContracts;

            this.Sells.Add(sold);

            var credit = (sold.NumberOfContracts * sold.Premium);

            PremiumReceived += credit;

            this.Transactions.Add(
                Transaction.CreditTx(
                    this.Id,
                    sold.Id,
                    this.Ticker,
                    $"Sold {sold.NumberOfContracts} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contract(s) for ${sold.Premium} premium/contract",
                    credit,
                    sold.When,
                    true
                )
            );

            ApplyClosedLogicIfApplicable(sold.When, sold.Id);
        }

        private void AddNoteIfNotEmpty(string notes)
        {
            if (!string.IsNullOrEmpty(notes)) this.Notes.Add(notes);
        }

        private void ApplyClosedLogicIfApplicable(DateTimeOffset when, Guid eventId)
        {
            if (this.NumberOfContracts != 0)
            {
                return;
            }

            this.Closed = when;

            var profit = PremiumReceived - PremiumPaid;

            var description = $"${this.StrikePrice.ToString("0.00")} {OptionType.ToString()}";

            this.Transactions.Add(
                Transaction.PLTx(this.Id, this.Ticker, description, PremiumPaid, PremiumReceived, when, true)
            );
        }

        private void ApplyFirstTransactionLogic(bool soldToOpen, DateTimeOffset filled)
        {
            this.Days = Math.Floor(this.Expiration.Subtract(filled).TotalDays);
            this.SoldToOpen = soldToOpen;
            this.FirstFill = filled;
        }

        internal void ApplyInternal(OptionOpened opened)
        {
            this.Id = opened.AggregateId;
            this.Ticker = opened.Ticker;
            this.StrikePrice = opened.StrikePrice;
            this.Expiration = opened.Expiration;
            this.OptionType = opened.OptionType;
            this.UserId = opened.UserId;
        }

        internal bool IsMatch(string ticker, decimal strike, OptionType type, DateTimeOffset expiration)
        {
            return this.Ticker == ticker 
                && this.StrikePrice == strike 
                && this.OptionType == type 
                && this.Expiration.Date == expiration.Date;
        }

        internal void ApplyInternal(OptionPurchased purchased)
        {
            if (this.FirstFill == null)
            {
                ApplyFirstTransactionLogic(false, purchased.When);
            }

            if (this.Deleted == true)
            {
                this.Deleted = false;
            }

            if (this.NumberOfContracts == 0)
            {
                this.PremiumReceived = 0;
                this.PremiumPaid = 0;
            }

            AddNoteIfNotEmpty(purchased.Notes);

            this.NumberOfContracts += purchased.NumberOfContracts;

            var debit = purchased.NumberOfContracts * purchased.Premium;

            this.PremiumPaid += debit;

            this.Buys.Add(purchased);

            var description = $"Bought {purchased.NumberOfContracts} x ${this.StrikePrice} {this.OptionType} {this.Expiration.ToString("MM/dd")} contracts for ${purchased.Premium}/contract";

            this.Transactions.Add(
                Transaction.DebitTx(
                    this.Id,
                    purchased.Id,
                    this.Ticker,
                    description,
                    debit,
                    purchased.When,
                    true
                )
            );

            ApplyClosedLogicIfApplicable(purchased.When, purchased.Id);
        }

        internal void ApplyInternal(OptionExpired expired)
        {
            this.NumberOfContracts = 0;

            this.Expirations.Add(expired);

            ApplyClosedLogicIfApplicable(expired.When, expired.Id);
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }
    }
}