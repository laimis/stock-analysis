using System;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class UserConfirmed : AggregateEvent, INotification
    {
        public UserConfirmed(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class UserCreated : AggregateEvent, INotification
    {
        public UserCreated(Guid id, Guid aggregateId, DateTimeOffset when, string email, string firstname, string lastname)
            : base(id, aggregateId, when)
        {
            Email = email;
            Firstname = firstname;
            Lastname = lastname;
        }

        public string Email { get; }
        public string Firstname { get; }
        public string Lastname { get; }
    }

    internal class UserDeleted : AggregateEvent
    {
        public UserDeleted(Guid id, Guid aggregateId, DateTimeOffset when, string feedback) : base(id, aggregateId, when)
        {
            Feedback = feedback;
        }

        public string Feedback { get; }
    }

    [Obsolete("dropped this functionality from domain")]
    internal class UserLoggedIn : AggregateEvent
    {
        public UserLoggedIn(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    public class UserPasswordResetRequested : AggregateEvent, INotification
    {
        public UserPasswordResetRequested(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }

    internal class UserPasswordSet : AggregateEvent
    {
        public UserPasswordSet(Guid id, Guid aggregateId, DateTimeOffset when, string hash, string salt)
            : base(id, aggregateId, when)
        {
            Hash = hash;
            Salt = salt;
        }

        public string Hash { get; }
        public string Salt { get; }
    }

    internal class UserSubscribedToPlan : AggregateEvent
    {
        public UserSubscribedToPlan(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string planId,
            string customerId,
            string subscriptionId)
            : base(id, aggregateId, when)
        {
            PlanId = planId;
            CustomerId = customerId;
            SubscriptionId = subscriptionId;
        }

        public string PlanId { get; }
        public string CustomerId { get; }
        public string SubscriptionId { get; }
    }

    internal class UserConnectedToBrokerage : AggregateEvent
    {
        public UserConnectedToBrokerage(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string accessToken,
            string refreshToken,
            string tokenType,
            long expiresInSeconds,
            string scope,
            long refreshTokenExpiresInSeconds)
            : base(id, aggregateId, when)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenType = tokenType;
            ExpiresInSeconds = expiresInSeconds;
            Scope = scope;
            RefreshTokenExpiresInSeconds = refreshTokenExpiresInSeconds;
        }

        public string AccessToken { get; }
        public string RefreshToken { get; }
        public string TokenType { get; }
        public long ExpiresInSeconds { get; }
        public string Scope { get; }
        public long RefreshTokenExpiresInSeconds { get; }
    }

    internal class UserRefreshedBrokerageConnection : AggregateEvent
    {
        public UserRefreshedBrokerageConnection(
            Guid id,
            Guid aggregateId,
            DateTimeOffset when,
            string accessToken,
            string refreshToken,
            string tokenType,
            long expiresInSeconds,
            string scope,
            long refreshTokenExpiresInSeconds)
            : base(id, aggregateId, when)
        {
            AccessToken = accessToken;
            RefreshToken = refreshToken;
            TokenType = tokenType;
            ExpiresInSeconds = expiresInSeconds;
            Scope = scope;
            RefreshTokenExpiresInSeconds = refreshTokenExpiresInSeconds;
        }

        public string AccessToken { get; }
        public string RefreshToken { get; }
        public string TokenType { get; }
        public long ExpiresInSeconds { get; }
        public string Scope { get; }
        public long RefreshTokenExpiresInSeconds { get; }
    }

    internal class UserDisconnectedFromBrokerage : AggregateEvent
    {
        public UserDisconnectedFromBrokerage(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}