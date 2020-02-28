using System;
using core.Adapters.Subscriptions;

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
        public DateTimeOffset? Verified { get; private set; }
        public DateTimeOffset? LastLogin { get; private set; }
        public string SubscriptionLevel { get; private set; }

        public UserState()
        {
            this.SubscriptionLevel = "Free";
        }

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

        internal void Apply(UserLoggedIn l)
        {
            this.LastLogin = l.When;
        }

        internal void Apply(UserDeleted d)
        {
            this.Deleted = d.When;
            this.DeleteFeedback = d.Feedback;
        }

        internal void Apply(UserConfirmed d)
        {
            this.Verified = d.When;
        }

        internal void Apply(UserSubscribedToPlan p)
        {
            this.SubscriptionLevel = p.PlanId == Plans.Starter ? "Starter" : "Full";
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