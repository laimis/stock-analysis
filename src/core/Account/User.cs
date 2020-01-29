using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Account
{
    public class User : Aggregate
    {
        public UserState State => _state;
        private UserState _state = new UserState();

        public User(string email, string firstname, string lastname)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Email is required");
            }

            if (string.IsNullOrWhiteSpace(firstname))
            {
                throw new InvalidOperationException("Firstname is required");
            }

            if (string.IsNullOrWhiteSpace(lastname))
            {
                throw new InvalidOperationException("Lastname is required");
            }

            Apply(
                new UserCreated(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, email, firstname, lastname)
            );
        }

        public User(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

        protected override void Apply(AggregateEvent e)
        {
            this._events.Add(e);

            ApplyInternal(e);
        }

        private void ApplyInternal(dynamic obj)
        {
            this.ApplyInternal(obj);
        }

        private void ApplyInternal(UserCreated c)
        {
            this.State.Apply(c);
        }
    }
}