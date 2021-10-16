using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Stocks
{
    public class Settings
    {
        public class Command : RequestWithTicker<CommandResponse>
        {
            [Required]
            public string Category { get; set; }
        }

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

                var stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);
                if (stock == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find stock for settings change"
                    );
                }

                stock.UpdateSettings(cmd.Category);

                await _storage.Save(stock, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}