using System;

namespace core.Account.Responses
{
    public class AccountStatusView
    {
        public bool LoggedIn { get; set; }
        public Guid Username { get; set; }
        public bool Verified { get; set; }
        public DateTimeOffset Created { get; set; }
        public string Email { get; set; }
        public string Firstname { get; set; }
        public string Lastname { get; set; }
        public bool IsAdmin { get; set; }
        public string SubscriptionLevel { get; set; }
        public bool ConnectedToBrokerage { get; set; }
        public bool BrokerageAccessTokenExpired { get; set; }
    }
}