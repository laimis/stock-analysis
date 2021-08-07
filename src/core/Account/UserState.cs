using System;
using core.Adapters.Subscriptions;
using core.Shared;

namespace core.Account
{
    public class UserState : IAggregateState
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
        public bool IsPasswordAvailable => GetSalt() != null;

        public UserState()
        {
            this.SubscriptionLevel = "Free";
        }

        internal void ApplyInternal(UserCreated c)
        {
            this.Id = c.AggregateId;
            this.Created = c.When;
            this.Email = c.Email;
            this.Firstname = c.Firstname;
            this.Lastname = c.Lastname;
        }

        internal void ApplyInternal(UserPasswordSet p)
        {
            this.PasswordHash = p.Hash;
            this.Salt = p.Salt;
        }

        internal void ApplyInternal(UserLoggedIn l)
        {
            this.LastLogin = l.When;
        }

        internal void ApplyInternal(UserDeleted d)
        {
            this.Deleted = d.When;
            this.DeleteFeedback = d.Feedback;
        }

        internal void ApplyInternal(UserConfirmed d)
        {
            this.Verified = d.When;
        }

        internal void ApplyInternal(UserSubscribedToPlan p)
        {
            this.SubscriptionLevel = p.PlanId == Plans.Starter ? "Starter" : "Full";
        }

        internal void ApplyInternal(UserPasswordResetRequested r)
        {
        }

        internal bool PasswordHashMatches(string hash)
        {
            return this.PasswordHash == hash;
        }

        internal string GetSalt()
        {
            return this.Salt;
        }

        public void Apply(AggregateEvent e)
        {
            ApplyInternal(e);
        }

        protected void ApplyInternal(dynamic obj)
        {
            ApplyInternal(obj);
        }
    }
}