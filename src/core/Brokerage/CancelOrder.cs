using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;

namespace core.Brokerage
{
    public class CancelOrder
    {
        public class Command : RequestWithUserId<CommandResponse>
        {
            [Required]
            public string OrderId { get; set; }

            public Command(string orderId, Guid userId) : base(userId)
            {
                OrderId = orderId;
            }
        }

        public class Handler : HandlerWithStorage<Command, CommandResponse>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(IAccountStorage accounts, IBrokerage brokerage, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<CommandResponse> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find user account for stock operation");
                }

                await _brokerage.CancelOrder(user.State, cmd.OrderId);

                return CommandResponse.Success();
            }
        }
    }
}