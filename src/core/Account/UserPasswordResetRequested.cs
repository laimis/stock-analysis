using System;
using core.Shared;

namespace core.Account
{
    internal class UserPasswordResetRequested : AggregateEvent
    {
        public UserPasswordResetRequested(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}