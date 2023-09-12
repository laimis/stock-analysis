using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;

namespace core.Stocks.Handlers
{
    public class DeleteStop
    {
        public class Command : RequestWithTicker<CommandResponse>
        {
            public Command(string ticker)
            {
                Ticker = ticker;
            }
        }

        public class Handler : HandlerWithStorage<Command, CommandResponse>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
                => _accounts = accounts;

            public override async Task<CommandResponse> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find user account for stock operation");
                }

                var stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find stock for settings change"
                    );
                }

                stock.DeleteStop();

                await _storage.Save(stock, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}