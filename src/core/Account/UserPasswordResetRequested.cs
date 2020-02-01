using System;
using core.Shared;

namespace core.Account
{
    public class UserPasswordResetRequested : AggregateEvent
    {
        public UserPasswordResetRequested(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}