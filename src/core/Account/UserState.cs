using System;

namespace core.Account
{
    public class UserState
    {
        public Guid Id { get; private set; }
        public DateTimeOffset Created { get; private set; }
        public string Email { get; private set; }
        public string Firstname { get; private set; }
        public string Lastname { get; private set; }
        private string PasswordHash { get; set; }
        private string Salt { get; set; }
        public DateTimeOffset? Deleted { get; private set; }
        public string DeleteFeedback { get; private set; }
        public DateTimeOffset? Confirmed { get; private set; }

        internal void Apply(UserCreated c)
        {
            this.Id = c.AggregateId;
            this.Created = c.When;
            this.Email = c.Email;
            this.Firstname = c.Firstname;
            this.Lastname = c.Lastname;
        }

        internal void Apply(UserPasswordSet p)
        {
            this.PasswordHash = p.Hash;
            this.Salt = p.Salt;
        }

        internal void Apply(UserDeleted d)
        {
            this.Deleted = d.When;
            this.DeleteFeedback = d.Feedback;
        }

        internal void Apply(UserConfirmed d)
        {
            this.Confirmed = d.When;
        }

        internal bool PasswordHashMatches(string hash)
        {
            return this.PasswordHash == hash;
        }

        internal string GetSalt()
        {
            return this.Salt;
        }
    }
}