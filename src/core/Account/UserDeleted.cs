using System;
using core.Shared;

namespace core.Account
{
    internal class UserDeleted : AggregateEvent
    {
        public UserDeleted(Guid id, Guid aggregateId, DateTimeOffset when, string feedback) : base(id, aggregateId, when)
        {
            this.Feedback = feedback;
        }

        public string Feedback { get; }
    }
}