using System;
using core.Shared;

namespace core.Account
{
    internal class UserConfirmed : AggregateEvent
    {
        public UserConfirmed(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}