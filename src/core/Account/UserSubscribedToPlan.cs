using System;
using core.Shared;

namespace core.Account
{
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
            this.PlanId = planId;
            this.CustomerId = customerId;
            this.SubscriptionId = subscriptionId;
        }

        public string PlanId { get; }
        public string CustomerId { get; }
        public string SubscriptionId { get; }
    }
}