using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class SoldOption : Aggregate
    {
        private SoldOptionState _state = new SoldOptionState();
        public SoldOptionState State => _state;

        public SoldOption(
            string ticker,
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

            var daysToExpiration = expiration.Subtract(DateTimeOffset.UtcNow).TotalDays;
            if (daysToExpiration <= 0)
            {
                throw new InvalidOperationException("Expiration date is in the past");
            }

            if (daysToExpiration > 365)
            {
                throw new InvalidOperationException("Expiratoin date is too far in the future");
            }

            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (premium <= 0)
            {
                throw new InvalidOperationException("Premium cannot be zero or negative");
            }

            Apply(new OptionSold(Guid.NewGuid(), Guid.NewGuid(), filled, ticker, type, strikePrice, expiration.Date, userId, amount, premium));
        }

        public SoldOption(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public void Close(int amount, double money, DateTimeOffset closed)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (this.State.Amount < amount)
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

        public static string GenerateKey(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice)
        {
            return $"{ticker}:{optionType}:{strikePrice}:{expiration.Date.ToString("yyyy-MM-dd")}";
        }
    }
}