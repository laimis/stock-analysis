using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Stocks
{
    public class Buy
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

                if (user.State.Verified == null)
                {
                    return CommandResponse.Failed(
                        "Please verify your email first before you can record buy transaction");
                }

                var stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);

                if (stock == null)
                {
                    stock = new OwnedStock(cmd.Ticker, cmd.UserId);
                }

                stock.Purchase(cmd.NumberOfShares, cmd.Price, cmd.Date.Value, cmd.Notes, cmd.StopPrice);

                await _storage.Save(stock, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}