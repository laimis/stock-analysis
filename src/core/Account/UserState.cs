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

        internal void Apply(UserCreated c)
        {
            this.Id = c.AggregateId;
            this.Created = c.When;
            this.Email = c.Email;
            this.Firstname = c.Firstname;
            this.Lastname = c.Lastname;
        }
    }
}