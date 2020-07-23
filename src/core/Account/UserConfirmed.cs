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
}