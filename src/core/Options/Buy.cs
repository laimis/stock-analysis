using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using MediatR;

namespace core.Options
{
    public class Buy
    {
        public class Command : OptionTransaction
        {
        }

        public class Handler : IRequestHandler<Command, Guid>
        {
            private IPortfolioStorage _storage;
            private IAccountStorage _accountStorage;

            public Handler(IAccountStorage accountStorage, IPortfolioStorage storage)
            {
                _storage = storage;
                _accountStorage = accountStorage;
            }
            
            public async Task<Guid> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(cmd.UserId);
                if (user == null)
                {
                    return Guid.Empty;
                }

                // TODO: return error
                if (!user.IsConfirmed)
                {
                    return Guid.Empty;
                }

                var optionType = (OptionType)Enum.Parse(typeof(OptionType), cmd.OptionType);
                
                var options = await _storage.GetOwnedOptions(cmd.UserId);
                var option = options.SingleOrDefault(o => o.IsMatch(cmd.Ticker, cmd.StrikePrice, optionType, cmd.ExpirationDate.Value));

                if (option == null)
                {
                    option = new OwnedOption(
                        cmd.Ticker,
                        cmd.StrikePrice,
                        optionType,
                        cmd.ExpirationDate.Value,
                        cmd.UserId);
                }

                option.Buy(cmd.NumberOfContracts, cmd.Premium, cmd.Filled.Value);

                await this._storage.Save(option, cmd.UserId);

                return option.State.Id;
            }
        }
    }
}