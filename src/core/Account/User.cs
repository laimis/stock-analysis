using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Account
{
    public class User : Aggregate
    {
        public UserState State { get; } = new UserState();
        public override IAggregateState AggregateState => State;

        public User(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        public User(string email, string firstname, string lastname)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(firstname))
            {
                throw new InvalidOperationException("First name is required");
            }

            if (string.IsNullOrWhiteSpace(lastname))
            {
                throw new InvalidOperationException("Last name is required");
            }

            Apply(
                new UserCreated(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    email.ToLowerInvariant(),
                    firstname,
                    lastname)
            );
        }

        public void SubscribeToPlan(string planId, string customerId, string subscriptionId)
        {
            Apply(
                new UserSubscribedToPlan(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, planId, customerId, subscriptionId)
            );
        }

        public void LoggedIn(string ipAddress, DateTimeOffset when)
        {
            Apply(
                new UserLoggedIn(Guid.NewGuid(), this.Id, when)
            );
        }

        public void Delete(string feedback)
        {
            Apply(
                new UserDeleted(Guid.NewGuid(), this.Id, DateTimeOffset.UtcNow, feedback)
            );
        }

        public bool PasswordHashMatches(string hash)
        {
            return this.State.PasswordHashMatches(hash);
        }

        public void Confirm()
        {
            if (this.State.Verified != null)
            {
                return;
            }
            
            Apply(
                new UserConfirmed(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow)
            );
        }

        public void SetPassword(string hash, string salt)
        {
            Apply(
                new UserPasswordSet(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, hash, salt)
            );
        }

        public void RequestPasswordReset(DateTimeOffset when)
        {
            Apply(
                new UserPasswordResetRequested(Guid.NewGuid(), this.State.Id, when)
            );
        }
    }
}