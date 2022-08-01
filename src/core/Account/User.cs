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
                new UserSubscribedToPlan(Guid.NewGuid(), Id, DateTimeOffset.UtcNow, planId, customerId, subscriptionId)
            );
        }

        public void LoggedIn(string ipAddress, DateTimeOffset when)
        {
            Apply(
                new UserLoggedIn(Guid.NewGuid(), Id, when)
            );
        }

        public void Delete(string feedback)
        {
            Apply(
                new UserDeleted(Guid.NewGuid(), Id, DateTimeOffset.UtcNow, feedback)
            );
        }

        internal void ConnectToBrokerage(string access_token, string refresh_token, string token_type, long expires_in_seconds, string scope, long refresh_token_expires_in_seconds)
        {
            Apply(
                new UserConnectedToBrokerage(Guid.NewGuid(), Id, DateTimeOffset.UtcNow, access_token, refresh_token, token_type, expires_in_seconds, scope, refresh_token_expires_in_seconds)
            );
        }

        public bool PasswordHashMatches(string hash)
        {
            return State.PasswordHashMatches(hash);
        }

        public void Confirm()
        {
            if (State.Verified != null)
            {
                return;
            }
            
            Apply(
                new UserConfirmed(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow)
            );
        }

        public void SetPassword(string hash, string salt)
        {
            Apply(
                new UserPasswordSet(Guid.NewGuid(), State.Id, DateTimeOffset.UtcNow, hash, salt)
            );
        }

        public void RequestPasswordReset(DateTimeOffset when)
        {
            Apply(
                new UserPasswordResetRequested(Guid.NewGuid(), State.Id, when)
            );
        }
    }
}