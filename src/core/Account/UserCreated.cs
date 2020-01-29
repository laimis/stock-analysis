using System;
using core.Shared;

namespace core.Account
{
    internal class UserCreated : AggregateEvent
    {
        public UserCreated(Guid id, Guid aggregateId, DateTimeOffset when, string email, string firstname, string lastname)
            : base(id, aggregateId, when)
        {
            this.Email = email;
            this.Firstname = firstname;
            this.Lastname = lastname;
        }

        public string Email { get; }
        public string Firstname { get; }
        public string Lastname { get; }
    }
}