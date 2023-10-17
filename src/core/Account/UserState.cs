using System;
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
        public string SubscriptionLevel { get; private set; } = "Free";
        public bool IsPasswordAvailable => GetSalt() != null;
        public string Name => $"{Firstname} {Lastname}";
        public string BrokerageAccessToken { get; private set; }
        public string BrokerageRefreshToken { get; private set; }
        public DateTimeOffset BrokerageAccessTokenExpires { get; private set; }
        public DateTimeOffset BrokerageRefreshTokenExpires { get; private set; }
        public bool ConnectedToBrokerage { get; private set; }
        public bool BrokerageAccessTokenExpired => BrokerageAccessTokenExpires < DateTimeOffset.UtcNow;
        public decimal? MaxLoss { get; private set; }

        internal void ApplyInternal(UserCreated c)
        {
            Id = c.AggregateId;
            Created = c.When;
            Email = c.Email;
            Firstname = c.Firstname;
            Lastname = c.Lastname;
        }

        public void ApplyInternal(UserPasswordSet p)
        {
            PasswordHash = p.Hash;
            Salt = p.Salt;
        }

        [Obsolete("No longer tracking this")]
        internal void ApplyInternal(UserLoggedIn l)
        {
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
        
        // TODO: comment out once this is moved to core.fs
        internal void ApplyInternal(UserSubscribedToPlan p)
        {
            // SubscriptionLevel = p.PlanId == Plans.Starter ? "Starter" : "Full";
        }

        internal void ApplyInternal(UserPasswordResetRequested _)
        {
            // no state to modify, this event gets captured via INotification mechanism
        }

        private void RefreshBrokerageData(string accessToken, string refreshToken, DateTimeOffset eventTimestamp)
        {
            ConnectedToBrokerage = true;
            BrokerageAccessToken = accessToken;
            BrokerageRefreshToken = refreshToken;
            BrokerageAccessTokenExpires = eventTimestamp.AddSeconds(1800);
            BrokerageRefreshTokenExpires = eventTimestamp.AddDays(90);
        }

        internal void ApplyInternal(UserConnectedToBrokerage e) =>
            RefreshBrokerageData(e.AccessToken, e.RefreshToken, e.When);
        
        internal void ApplyInternal(UserRefreshedBrokerageConnection e) =>
            RefreshBrokerageData(e.AccessToken, e.RefreshToken, e.When);

        internal void ApplyInternal(UserDisconnectedFromBrokerage _)
        {
            ConnectedToBrokerage = false;
            BrokerageAccessToken = null;
            BrokerageRefreshToken = null;
            BrokerageAccessTokenExpires = DateTimeOffset.MinValue;
        }

        internal void ApplyInternal(UserSettingSet e)
        {
            switch (e.Key)
            {
                case "maxLoss":
                    var maxLoss = Decimal.Parse(e.Value);
                    MaxLoss = maxLoss;
                    break;
                default:
                    throw new InvalidOperationException($"Unknown setting: {e.Key}");
            }
        }

        public bool PasswordHashMatches(string hash)
        {
            return PasswordHash == hash;
        }

        public string GetSalt()
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