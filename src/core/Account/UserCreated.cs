using System;
using core.Shared;

namespace core.Account
{
    internal class UserCreated : AggregateEvent
    {
        public UserCreated(Guid id, Guid aggregateId, DateTimeOffset when, string email)
            : base(id, aggregateId, when)
        {
            this.Email = email;
        }

        public string Email { get; }
    }
}