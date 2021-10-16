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
}