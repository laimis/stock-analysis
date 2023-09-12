using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;

namespace core.Cryptos.Handlers
{
    public class Buy
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
                    crypto = new OwnedCrypto(cmd.Token, cmd.UserId);
                }

                crypto.Purchase(
                    quantity: cmd.Quantity, dollarAmountSpent: cmd.DollarAmount, date: cmd.Date.Value, notes: cmd.Notes
                );

                await _storage.Save(crypto, cmd.UserId);

                return CommandResponse.Success();
            }
        }
    }
}