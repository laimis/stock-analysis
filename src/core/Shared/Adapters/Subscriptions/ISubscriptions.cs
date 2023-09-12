using core.Account;

namespace core.Shared.Adapters.Subscriptions
{
    public interface ISubscriptions
    {
        SubscriptionResult Create(User user, string email, string paymentToken, string planId);
    }
}