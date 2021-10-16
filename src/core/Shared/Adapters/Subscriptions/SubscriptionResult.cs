namespace core.Adapters.Subscriptions
{
    public class SubscriptionResult
    {
        public SubscriptionResult(string customerId, string subscriptionId)
        {
            CustomerId = customerId;
            SubscriptionId = subscriptionId;
        }
        
        public string CustomerId { get; }
        public string SubscriptionId { get; }
    }
}