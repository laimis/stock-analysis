using System;
using core.Shared;

namespace core.Account
{
    internal class UserPasswordSet : AggregateEvent
    {
        public UserPasswordSet(Guid id, Guid aggregateId, DateTimeOffset when, string hash, string salt)
            : base(id, aggregateId, when)
        {
            this.Hash = hash;
            this.Salt = salt;
        }

        public string Hash { get; }
        public string Salt { get; }
    }
}