using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Utils;
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

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<Guid> Handle(Command cmd, CancellationToken cancellationToken)
            {
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

                await this._storage.Save(option, cmd.UserId);

                return option.State.Id;
            }
        }
    }
}