using core.Account;

namespace core.Adapters.Subscriptions
{
    public interface ISubscriptions
    {
        SubscriptionResult Create(User user, string email, string paymentToken, string planId);
    }
}