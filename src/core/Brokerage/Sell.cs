using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;

namespace core.Brokerage
{
    public class Sell
    {
        public class Command : RequestWithTicker<CommandResponse>
        {
            [Range(1, 1000)]
            public decimal NumberOfShares { get; set; }
            
            [Range(0, 2000)]
            public decimal Price { get; set; }
            
            [Required]
            public BrokerageOrderType Type { get; set; }

            [Required]
            public BrokerageOrderDuration Duration  { get; set; }
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

                var response = await _brokerage.SellOrder(user.State, cmd.Ticker, cmd.NumberOfShares, cmd.Price, cmd.Type, cmd.Duration);

                return response.IsOk switch {
                    true => CommandResponse.Success(),
                    false => CommandResponse.Failed(response.Error.Message)
                };
            }
        }
    }
}