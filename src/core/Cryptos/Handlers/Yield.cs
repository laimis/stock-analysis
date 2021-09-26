using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Cryptos.Handlers
{
    public class Yield
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

                var crypto = await this._storage.GetCrypto(cmd.Token, cmd.UserId);
                if (crypto == null)
                {
                    return CommandResponse.Failed(
                        $"You don't have {cmd.Token.ToString()} to start earning yield, record buy or award transaction first");
                }

                crypto.Yield(
                    quantity: cmd.Quantity,
                    dollarAmountWorth: cmd.DollarAmount,
                    date: cmd.Date.Value,
                    notes: cmd.Notes);

                await this._storage.Save(crypto, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}