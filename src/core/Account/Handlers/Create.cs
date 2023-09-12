using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Shared.Adapters.Subscriptions;
using MediatR;

namespace core.Account.Handlers
{
    public class Create
    {
        public class Command : IRequest<CommandResponse<User>>
        {
            [Required]
            public UserInfo UserInfo { get; set; }

            public PaymentInfo PaymentInfo { get; set; }
        }

        public class UserInfo : IRequest<CommandResponse<User>>
        {
            [Required]
            public string Email { get; set; }
            
            [Required]
            public string Firstname { get; set; }
            
            [Required]
            public string Lastname { get; set; }
            
            [Required]
            [MinLength(10)]
            [MaxLength(1000)]
            public string Password { get; set; }

            [Required]
            [Range(typeof(bool), "true", "true", ErrorMessage = "You must agree to Terms & Conditions")]
            public bool Terms { get; set; }
        }

        public class PaymentInfo
        {
            [Required]
            public PaymentToken Token { get; set; }
            [Required]
            public string PlanId { get; set; }
        }

        public class PaymentToken
        {
            [Required]
            public string Id { get; set; }

            [Required]
            public string Email { get; set; }
        }

        public class Handler : IRequestHandler<Command, CommandResponse<User>>
        {
            private IAccountStorage _storage;
            private IPasswordHashProvider _hash;
            private ISubscriptions _subscriptions;

            public Handler(IAccountStorage storage, IPasswordHashProvider hash, ISubscriptions subscriptions)
            {
                _storage = storage;
                _hash = hash;
                _subscriptions = subscriptions;
            }

            public async Task<CommandResponse<User>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var exists = await _storage.GetUserByEmail(cmd.UserInfo.Email);
                if (exists != null)
                {
                    return CommandResponse<User>.Failed($"Account with {cmd.UserInfo} already exists");
                }

                var u = new User(cmd.UserInfo.Email, cmd.UserInfo.Firstname, cmd.UserInfo.Lastname);

                var (hash, salt) = _hash.Generate(cmd.UserInfo.Password, 32);

                u.SetPassword(hash, salt);

                if (cmd.PaymentInfo != null)
                {
                    var result = _subscriptions.Create(
                        u,
                        planId: cmd.PaymentInfo.PlanId,
                        paymentToken: cmd.PaymentInfo.Token.Id,
                        email: cmd.PaymentInfo.Token.Email);

                    if (result.CustomerId != null)
                    {
                        u.SubscribeToPlan(cmd.PaymentInfo.PlanId, result.CustomerId, result.SubscriptionId);
                    }
                    else
                    {
                        return CommandResponse<User>.Failed(
                            $"Failed to process the payment, please try again or use a different payment form"
                        );
                    }
                }

                await _storage.Save(u);

                return CommandResponse<User>.Success(u);
            }
        }
    }
}