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
            PositionType positionType,
            OptionType type,
            DateTimeOffset expiration,
            double strikePrice,
            string userId,
            int amount,
            double premium,
            DateTimeOffset filled)
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

            if (expiration < filled)
            {
                throw new InvalidOperationException("Expiration date cannot be before filled date");
            }

            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (premium <= 0)
            {
                throw new InvalidOperationException("Premium cannot be zero or negative");
            }

            Apply(new OptionOpened(
                Guid.NewGuid(),
                Guid.NewGuid(),
                filled,
                ticker,
                positionType,
                type,
                strikePrice,
                expiration.Date,
                userId,
                amount,
                premium));
        }

        public OwnedOption(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public void Close(int amount, double money, DateTimeOffset closed)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (this.State.NumberOfContracts < amount)
            {
                throw new InvalidOperationException("Don't have enough options to close");
            }

            if (money < 0)
            {
                throw new InvalidOperationException("Close money cannot be negative");
            }

            Apply(new OptionClosed(Guid.NewGuid(), this.State.Id, closed, amount, money));
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

        protected void ApplyInternal(OptionClosed closed)
        {
            this.State.Apply(closed);
        }

        protected void ApplyInternal(OptionOpened closed)
        {
            this.State.Apply(closed);
        }

        public static string GenerateKey(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice)
        {
            return $"{ticker}:{optionType}:{strikePrice}:{expiration.Date.ToString("yyyy-MM-dd")}";
        }
    }
}