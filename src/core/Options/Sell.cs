using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;

namespace core.Options
{
    public class Sell
    {
        public class Command : OptionTransaction
        {
        }

        public class Handler : HandlerWithStorage<Command, Guid>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Guid> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return Guid.Empty;
                }

                // TODO: return error
                if (!user.IsConfirmed)
                {
                    return Guid.Empty;
                }

                var options = await _storage.GetOwnedOptions(cmd.UserId);

                var type = (OptionType)Enum.Parse(typeof(OptionType), cmd.OptionType);

                var option = options.SingleOrDefault(o => o.IsMatch(cmd.Ticker, cmd.StrikePrice, type, cmd.ExpirationDate.Value));
                if (option == null)
                {
                    option = new OwnedOption(
                        cmd.Ticker,
                        cmd.StrikePrice,
                        type,
                        cmd.ExpirationDate.Value,
                        cmd.UserId
                    );
                }

                option.Sell(cmd.NumberOfContracts, cmd.Premium, cmd.Filled.Value);

                await _storage.Save(option, cmd.UserId);

                return option.State.Id;
            }
        }
    }
}