using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOption : Aggregate
    {
        private OwnedOptionState _state = new OwnedOptionState();
        public OwnedOptionState State => _state;

        public OwnedOption(
            string ticker,
            double strikePrice,
            OptionType type,
            DateTimeOffset expiration,
            string userId)
        {
            if (string.IsNullOrWhiteSpace(ticker))
            {
                throw new InvalidOperationException("Missing ticker value");
            }

            if (string.IsNullOrWhiteSpace(userId))
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
        {
            return this.State.IsMatch(ticker, strike, type, expiration);
        }

        public OwnedOption(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public void Buy(int amount, double premium, DateTimeOffset filled)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (premium < 0)
            {
                throw new InvalidOperationException("Premium money cannot be negative");
            }

            Apply(new OptionPurchased(Guid.NewGuid(), this.State.Id, filled, amount, premium));
        }

        public void Sell(int numberOfContracts, double premium, DateTimeOffset filled)
        {
            if (numberOfContracts <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (premium < 0)
            {
                throw new InvalidOperationException("Premium money cannot be negative");
            }

            Apply(new OptionSold(Guid.NewGuid(), this.State.Id, filled, numberOfContracts, premium));
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
    }
}