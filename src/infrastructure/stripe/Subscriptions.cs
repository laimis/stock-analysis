using System;
using System.Collections.Generic;
using core.Account;
using core.Shared.Adapters.Subscriptions;
using Stripe;

namespace stripe
{
    public class Subscriptions : ISubscriptions
    {
        public Subscriptions(string stripeKey)
        {
            StripeConfiguration.ApiKey = stripeKey;
        }

        public SubscriptionResult Create(User user, string email, string paymentToken, string planId)
        {
            var paymentMethodCreate = new PaymentMethodCreateOptions {
                Card = new PaymentMethodCardCreateOptions{
                    Token = paymentToken
                },
                Type = "card"
            };

            var pmService = new PaymentMethodService();
            var paymentMethod = pmService.Create(paymentMethodCreate);

            Console.WriteLine("Payment method: " + paymentMethod.Id);

            var custOptions = new CustomerCreateOptions {
                Email = email,
                PaymentMethod = paymentMethod.Id,
                InvoiceSettings = new CustomerInvoiceSettingsOptions {
                        DefaultPaymentMethod = paymentMethod.Id,
                    },
                Metadata = new Dictionary<string, string> {
                    { "userid", user.Id.ToString()}
                }
            };

            var custService = new CustomerService();
            var customer = custService.Create(custOptions);

            Console.WriteLine("Customer: " + customer.Id);

            var items = new List<SubscriptionItemOptions> {
                new SubscriptionItemOptions {
                    Plan = planId,
                }
            };
            
            var subscriptionOptions = new SubscriptionCreateOptions {
                Customer = customer.Id,
                Items = items,
                TrialPeriodDays = 7
            };
            
            subscriptionOptions.AddExpand("latest_invoice.payment_intent");

            var subService = new SubscriptionService();

            var subscription = subService.Create(subscriptionOptions);

            Console.WriteLine("Subscription: " + subscription.Id);

            return new SubscriptionResult(
                customerId: customer.Id,
                subscriptionId: subscription.Id);
        }
    }
}
