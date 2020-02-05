using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Stocks
{
    public class Sell
    {
        public class Command : StockTransaction {}

        public class Handler : HandlerWithStorage<Command, CommandResponse>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<CommandResponse> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find user account for stock operation");
                }

                if (!user.Verified)
                {
                    return CommandResponse.Failed(
                        "Please verify your email first before you can record sell transaction");
                }

                var stock = await this._storage.GetStock(cmd.Ticker, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse.Failed(
                        $"You don't have shares of {cmd.Ticker.ToString()} to sell, record buy transaction first");
                }

                stock.Sell(cmd.NumberOfShares, cmd.Price, cmd.Date.Value);

                await this._storage.Save(stock, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}