using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Utils;

namespace core.Brokerage
{
    public class Buy
    {
        public class Command : RequestWithTicker<CommandResponse>
        {
            [Range(1, 1000)]
            public decimal NumberOfShares { get; set; }
            
            [Range(0, 2000)]
            public decimal Price { get; set; }
            
            [Required]
            [ValidValues("limit", "market")]
            public string Type { get; set; }
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

                await _brokerage.BuyOrder(user.State, cmd.Ticker, cmd.NumberOfShares, cmd.Price, cmd.Type);

                return CommandResponse.Success();
            }
        }
    }
}