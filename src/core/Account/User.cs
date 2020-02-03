using System;
using System.Collections.Generic;
using core.Shared;

namespace core.Account
{
    public class User : Aggregate
    {
        public UserState State => _state;
        private UserState _state = new UserState();

        public override Guid Id => State.Id;
        public bool IsPasswordAvailable => State.GetSalt() != null;
        public bool Verified => State.Verified != null;
        public DateTimeOffset Created => State.Created;
        public string Email => State.Email;
        public string Firstname => State.Firstname;
        public string Lastname => State.Lastname;

        public User(IEnumerable<AggregateEvent> events) : base(events)
        {
        }

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
                new UserCreated(
                    Guid.NewGuid(),
                    Guid.NewGuid(),
                    DateTimeOffset.UtcNow,
                    email.ToLowerInvariant(),
                    firstname,
                    lastname)
            );
        }

        public void Delete(string feedback)
        {
            Apply(
                new UserDeleted(Guid.NewGuid(), Guid.NewGuid(), DateTimeOffset.UtcNow, feedback)
            );
        }

        public bool PasswordHashMatches(string hash)
        {
            return this.State.PasswordHashMatches(hash);
        }

        public void Confirm()
        {
            if (this.State.Verified != null)
            {
                return;
            }
            
            Apply(
                new UserConfirmed(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow)
            );
        }

        public void SetPassword(string hash, string salt)
        {
            Apply(
                new UserPasswordSet(Guid.NewGuid(), this.State.Id, DateTimeOffset.UtcNow, hash, salt)
            );
        }

        public void RequestPasswordReset(DateTimeOffset when)
        {
            Apply(
                new UserPasswordResetRequested(Guid.NewGuid(), this.State.Id, when)
            );
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

        private void ApplyInternal(UserPasswordSet p)
        {
            this.State.Apply(p);
        }

        private void ApplyInternal(UserPasswordResetRequested r)
        {
        }

        private void ApplyInternal(UserDeleted d)
        {
            this.State.Apply(d);
        }

        private void ApplyInternal(UserConfirmed c)
        {
            this.State.Apply(c);
        }
    }
}