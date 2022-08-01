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
        public string Name => $"{Firstname} {Lastname}";
        public string BrokerageAccessToken { get; private set; }
        public string BrokerageRefreshToken { get; private set; }
        public DateTimeOffset BrokerageAccessTokenExpires { get; private set; }
        public bool ConnectedToBrokerage { get; private set; }
        public bool BrokerageAccessTokenExpired => BrokerageAccessTokenExpires < DateTimeOffset.UtcNow;

        public UserState()
        {
            SubscriptionLevel = "Free";
        }

        internal void ApplyInternal(UserCreated c)
        {
            Id = c.AggregateId;
            Created = c.When;
            Email = c.Email;
            Firstname = c.Firstname;
            Lastname = c.Lastname;
        }

        internal void ApplyInternal(UserPasswordSet p)
        {
            PasswordHash = p.Hash;
            Salt = p.Salt;
        }

        internal void ApplyInternal(UserLoggedIn l)
        {
            LastLogin = l.When;
        }

        internal void ApplyInternal(UserDeleted d)
        {
            Deleted = d.When;
            DeleteFeedback = d.Feedback;
        }

        internal void ApplyInternal(UserConfirmed d)
        {
            Verified = d.When;
        }

        internal void ApplyInternal(UserSubscribedToPlan p)
        {
            SubscriptionLevel = p.PlanId == Plans.Starter ? "Starter" : "Full";
        }

        internal void ApplyInternal(UserPasswordResetRequested r)
        {
        }

        internal void ApplyInternal(UserConnectedToBrokerage e)
        {
            ConnectedToBrokerage = true;
            BrokerageAccessToken = e.AccessToken;
            BrokerageRefreshToken = e.RefreshToken;
            BrokerageAccessTokenExpires = e.When.AddSeconds(e.ExpiresInSeconds);
        }

        internal bool PasswordHashMatches(string hash)
        {
            return PasswordHash == hash;
        }

        internal string GetSalt()
        {
            return Salt;
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