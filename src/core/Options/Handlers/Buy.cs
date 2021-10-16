using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Options
{
    public class Buy
    {
        public class Command : OptionTransaction
        {
        }

        public class Handler : IRequestHandler<Command, CommandResponse<OwnedOption>>
        {
            private IPortfolioStorage _storage;
            private IAccountStorage _accountStorage;

            public Handler(IAccountStorage accountStorage, IPortfolioStorage storage)
            {
                _storage = storage;
                _accountStorage = accountStorage;
            }
            
            public async Task<CommandResponse<OwnedOption>> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse<OwnedOption>.Failed(
                        "Unable to find user account for options operation");
                }

                if (user.State.Verified == null)
                {
                    return CommandResponse<OwnedOption>.Failed(
                        "Please verify your email first before you can record option transaction");
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

                option.Buy(cmd.NumberOfContracts, cmd.Premium, cmd.Filled.Value, cmd.Notes);

                await _storage.Save(option, cmd.UserId);

                return CommandResponse<OwnedOption>.Success(option);
            }
        }
    }
}