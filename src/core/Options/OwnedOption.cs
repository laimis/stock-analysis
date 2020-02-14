using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOption : Aggregate
    {
        private OwnedOptionState _state = new OwnedOptionState();
        public OwnedOptionState State => _state;
        public override Guid Id => State.Id;

        public OwnedOption(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public OwnedOption(
            Ticker ticker,
            double strikePrice,
            OptionType type,
            DateTimeOffset expiration,
            Guid userId)
        {
            if (userId == Guid.Empty)
            {
                throw new InvalidOperationException("Missing user id");
            }

            if (strikePrice <= 0)
            {
                throw new InvalidOperationException("Strike price cannot be zero or negative");
            }

            if (expiration == DateTimeOffset.MinValue)
            {
                throw new InvalidOperationException("Expiration date is in the past");
            }

            if (expiration == DateTimeOffset.MaxValue)
            {
                throw new InvalidOperationException("Expiration date is too far in the future");
            }

            Apply(new OptionOpened(
                Guid.NewGuid(),
                Guid.NewGuid(),
                DateTimeOffset.UtcNow,
                ticker,
                strikePrice,
                type,
                expiration.Date,
                userId));
        }

        public bool IsMatch(string ticker, double strike, OptionType type, DateTimeOffset expiration)
            => this.State.IsMatch(ticker, strike, type, expiration);

        public bool IsActive => !this.State.IsExpired && this.State.NumberOfContracts != 0;
        public string Ticker => this.State.Ticker;
        public string Description => $"{(State.NumberOfContracts > 0 ? "BOUGHT" : "SOLD")} {Math.Abs(State.NumberOfContracts)} ${State.StrikePrice} {State.OptionType} contracts";
        public DateTimeOffset Expiration => this.State.Expiration;
        public bool IsExpired => this.State.IsExpired;
        public bool ExpiresSoon => !IsExpired && this.State.DaysUntilExpiration >= 0 && this.State.DaysUntilExpiration < 7;
        public long? DaysLeft => this.State.DaysUntilExpiration;

        public void Buy(int numberOfContracts, double premium, DateTimeOffset filled, string notes)
        {
            if (numberOfContracts <= 0)
            {
                throw new InvalidOperationException("Number of contracts cannot be zero or negative");
            }

            if (premium < 0)
            {
                throw new InvalidOperationException("Premium amount cannot be negative");
            }

            Apply(
                new OptionPurchased(
                    Guid.NewGuid(),
                    this.State.Id,
                    filled,
                    this.State.UserId,
                    numberOfContracts,
                    premium,
                    notes
                )
            );
        }

        public void Sell(int numberOfContracts, double premium, DateTimeOffset filled, string notes)
        {
            if (numberOfContracts <= 0)
            {
                throw new InvalidOperationException("Number of contracts cannot be zero or negative");
            }

            if (premium < 0)
            {
                throw new InvalidOperationException("Premium money cannot be negative");
            }

            Apply(
                new OptionSold(
                    Guid.NewGuid(),
                    this.State.Id,
                    filled,
                    this.State.UserId,
                    numberOfContracts,
                    premium,
                    notes
                )
            );
        }

        public void Expire()
        {
            Apply(new OptionExpired(Guid.NewGuid(), this.State.Id, this.State.Expiration));
        }

        protected override void Apply(AggregateEvent e)
        {
            this._events.Add(e);

            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }

        protected void ApplyInternal(OptionSold sold)
        {
            this.State.Apply(sold);
        }

        protected void ApplyInternal(OptionPurchased purchased)
        {
            this.State.Apply(purchased);
        }

        protected void ApplyInternal(OptionOpened opened)
        {
            this.State.Apply(opened);
        }

        protected void ApplyInternal(OptionExpired expired)
        {
            this.State.Apply(expired);
        }
    }
}