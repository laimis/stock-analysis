using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;

namespace core.Cryptos.Handlers
{
    public class Sell
    {
        public class Command : CryptoTransaction {}

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

                var crypto = await _storage.GetCrypto(cmd.Token, cmd.UserId);
                if (crypto == null)
                {
                    return CommandResponse.Failed(
                        $"You don't have shares of {cmd.Token.ToString()} to sell, record buy transaction first");
                }

                crypto.Sell(
                    quantity: cmd.Quantity,
                    dollarAmountReceived: cmd.DollarAmount,
                    date: cmd.Date.Value,
                    notes: cmd.Notes);

                await _storage.Save(crypto, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}