using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOptionState : IAggregateState
    {
        public OwnedOptionState()
        {
            Transactions = new List<Transaction>();
            Buys = new List<OptionPurchased>();
            Sells = new List<OptionSold>();
            Expirations = new List<OptionExpired>();
            Notes = new List<string>();
        }

        public Guid Id { get; private set; }
        public string Ticker { get; internal set; }
        public decimal StrikePrice { get; internal set; }
        public DateTimeOffset Expiration { get; private set; }
        public bool IsExpired => DateTimeOffset.UtcNow.Date > Expiration;
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

        public long DaysUntilExpiration => IsExpired ? 0 :
            (long)Math.Ceiling(Math.Abs(Expiration.Subtract(DateTimeOffset.UtcNow).TotalDays));

        public DateTimeOffset? Closed { get; private set; }
        public int DaysHeld
        {
            get
            {
                DateTimeOffset date = DateTimeOffset.UtcNow;
                if (Closed != null)
                {
                    date = Closed.Value;
                }

                if (IsExpired)
                {
                    date = Expiration;
                }

                var val = (int)Math.Floor(date.Subtract(FirstFill.Value).TotalDays);
                if (val == 0)
                {
                    val = 1;
                }
                return val;
            }
        }
        
        public bool Assigned => Expirations.Count > 0 && Expirations[0].Assigned;
        public bool Active => !Deleted && !IsExpired && NumberOfContracts != 0;
        public bool ExpiresSoon => !IsExpired && DaysUntilExpiration >= 0 && DaysUntilExpiration < 7;
        public long? DaysLeft => DaysUntilExpiration;
        public bool Deleted { get; private set; }
        private decimal PremiumReceived { get; set; }
        private decimal PremiumPaid { get; set; }
        public List<string> Notes { get; }

        internal void ApplyInternal(OptionDeleted deleted)
        {
            NumberOfContracts = 0;
            Transactions.Clear();
            Buys.Clear();
            Sells.Clear();
            FirstFill = null;
            SoldToOpen = null;
            Closed = null;
            Notes.Clear();
            Expirations.Clear();

            Deleted = true;
        }

        internal void ApplyInternal(OptionSold sold)
        {
            if (SoldToOpen == null)
            {
                ApplyFirstTransactionLogic(true, sold.When);
            }

            if (Deleted == true)
            {
                Deleted = false;
            }

            AddNoteIfNotEmpty(sold.Notes);

            if (NumberOfContracts == 0)
            {
                PremiumReceived = 0;
                PremiumPaid = 0;
            }

            NumberOfContracts -= sold.NumberOfContracts;

            Sells.Add(sold);

            var credit = (sold.NumberOfContracts * sold.Premium);

            PremiumReceived += credit;

            Transactions.Add(
                Transaction.NonPLTx(
                    Id,
                    sold.Id,
                    Ticker,
                    $"Sold {sold.NumberOfContracts} x ${StrikePrice} {OptionType} {Expiration.ToString("MM/dd")} contract(s) for ${sold.Premium} premium/contract",
                    sold.Premium,
                    credit,
                    sold.When,
                    true
                )
            );

            ApplyClosedLogicIfApplicable(sold.When, sold.Id);
        }

        private void AddNoteIfNotEmpty(string notes)
        {
            if (!string.IsNullOrEmpty(notes)) Notes.Add(notes);
        }

        private void ApplyClosedLogicIfApplicable(DateTimeOffset when, Guid eventId)
        {
            if (NumberOfContracts != 0)
            {
                return;
            }

            Closed = when;

            var profit = PremiumReceived - PremiumPaid;

            var description = $"${StrikePrice.ToString("0.00")} {OptionType.ToString()}";

            Transactions.Add(
                Transaction.PLTx(Id, Ticker, description, profit, PremiumReceived - PremiumPaid, when, true)
            );
        }

        private void ApplyFirstTransactionLogic(bool soldToOpen, DateTimeOffset filled)
        {
            Days = Math.Floor(Expiration.Subtract(filled).TotalDays);
            SoldToOpen = soldToOpen;
            FirstFill = filled;
        }

        internal void ApplyInternal(OptionOpened opened)
        {
            Id = opened.AggregateId;
            Ticker = opened.Ticker;
            StrikePrice = opened.StrikePrice;
            Expiration = opened.Expiration;
            OptionType = opened.OptionType;
            UserId = opened.UserId;
        }

        internal bool IsMatch(string ticker, decimal strike, OptionType type, DateTimeOffset expiration)
        {
            return Ticker == ticker 
                && StrikePrice == strike 
                && OptionType == type 
                && Expiration.Date == expiration.Date;
        }

        internal void ApplyInternal(OptionPurchased purchased)
        {
            if (FirstFill == null)
            {
                ApplyFirstTransactionLogic(false, purchased.When);
            }

            if (Deleted == true)
            {
                Deleted = false;
            }

            if (NumberOfContracts == 0)
            {
                PremiumReceived = 0;
                PremiumPaid = 0;
            }

            AddNoteIfNotEmpty(purchased.Notes);

            NumberOfContracts += purchased.NumberOfContracts;

            var debit = purchased.NumberOfContracts * purchased.Premium;

            PremiumPaid += debit;

            Buys.Add(purchased);

            var description = $"Bought {purchased.NumberOfContracts} x ${StrikePrice} {OptionType} {Expiration.ToString("MM/dd")} contracts for ${purchased.Premium}/contract";

            Transactions.Add(
                Transaction.NonPLTx(
                    Id,
                    purchased.Id,
                    Ticker,
                    description,
                    purchased.Premium,
                    debit,
                    purchased.When,
                    true
                )
            );

            ApplyClosedLogicIfApplicable(purchased.When, purchased.Id);
        }

        internal void ApplyInternal(OptionExpired expired)
        {
            NumberOfContracts = 0;

            Expirations.Add(expired);

            ApplyClosedLogicIfApplicable(expired.When, expired.Id);
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }
    }
}