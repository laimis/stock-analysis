using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Options
{
    public class OwnedOption : Aggregate
    {
        public OwnedOptionState State { get; } = new OwnedOptionState();

        public override IAggregateState AggregateState => State;

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

        public void Delete()
        {
            Apply(
                new OptionDeleted(
                    Guid.NewGuid(),
                    this.Id,
                    DateTimeOffset.UtcNow
                )
            );
        }

        public bool IsMatch(string ticker, double strike, OptionType type, DateTimeOffset expiration)
            => this.State.IsMatch(ticker, strike, type, expiration);

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

            if (filled > this.State.Expiration)
            {
                throw new InvalidOperationException("Filled date cannot be past expiration");
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

        public void Expire(bool assigned)
        {
            if (this.State.Expirations.Count > 0)
            {
                throw new InvalidOperationException("You already marked this option as expired");
            }

            Apply(new OptionExpired(Guid.NewGuid(), this.State.Id, this.State.Expiration, assigned));
        }
    }

    public enum OptionType
    {
        CALL,
        PUT
    }
}