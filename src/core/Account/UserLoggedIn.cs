using System;
using core.Shared;

namespace core.Account
{
    internal class UserLoggedIn : AggregateEvent
    {
        public UserLoggedIn(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}