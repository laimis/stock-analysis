using System;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class UserPasswordResetRequested : AggregateEvent, INotification
    {
        public UserPasswordResetRequested(Guid id, Guid aggregateId, DateTimeOffset when) : base(id, aggregateId, when)
        {
        }
    }
}