using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Subscriptions;
using core.Shared;
using MediatR;

namespace core.Account
{
    public class Subscribe
    {
        public class Command : RequestWithUserId<CommandResponse<User>>
        {
            [Required]
            public PaymentToken Token { get; set; }
            [Required]
            public string PlanId { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<User>>
        {
            private IAccountStorage _storage;
            private ISubscriptions _subscriptions;

            public Handler(
                IAccountStorage storage,
                ISubscriptions subscriptions)
            {
                _storage = storage;
                _subscriptions = subscriptions;
            }

            public async Task<CommandResponse<User>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await this._storage.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse<User>.Failed($"Account does not exist");
                }

                var result = _subscriptions.Create(
                    user,
                    planId: cmd.PlanId,
                    paymentToken: cmd.Token.Id,
                    email: cmd.Token.Email);

                if (result.CustomerId != null)
                {
                    user.SubscribeToPlan(cmd.PlanId, result.CustomerId, result.SubscriptionId);

                    await _storage.Save(user);
                }

                return CommandResponse<User>.Success(user);
            }
        }
    }

    public class PaymentToken
    {
        [Required]
        public string Id { get; set; }

        [Required]
        public string Email {get; set; }
    }
}