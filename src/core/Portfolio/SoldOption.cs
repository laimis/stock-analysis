using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Portfolio
{
    public class SoldOption : Aggregate
    {
        private SoldOptionState _state = new SoldOptionState();
        public SoldOptionState State => _state;

        public SoldOption(string ticker, OptionType type, DateTimeOffset expiration, double strikePrice, string userId)
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

            Apply(new OptionObtained(ticker, type, strikePrice, expiration, userId, DateTime.UtcNow));
        }

        public SoldOption(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public void Open(int amount, double premium, DateTimeOffset filled)
        {
            if (amount <= 0)
            {
                throw new InvalidOperationException("Amount cannot be zero or negative");
            }

            if (premium <= 0)
            {
                throw new InvalidOperationException("Premium cannot be zero or negative");
            }

            Apply(new OptionOpened(this.State.Key, this.State.UserId, amount, premium, filled));
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

        protected void ApplyInternal(OptionObtained obtained)
        {
            this.State.Ticker = obtained.TickerSymbol;
            this.State.StrikePrice = obtained.StrikePrice;
            this.State.Expiration = obtained.Expiration;
            this.State.Type = obtained.Type;
            this.State.UserId = obtained.UserId;
        }

        protected void ApplyInternal(OptionOpened opened)
        {
            this.State.Amount++;
            this.State.Filled = opened.When;
            this.State.Premium = opened.Premium;
        }

        public static string GenerateKey(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice)
        {
            return $"{ticker}:{optionType}:{strikePrice}:{expiration}";
        }
    }
}