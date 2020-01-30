using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace core.Options
{
    public class Sell
    {
        public class Command : OptionTransaction
        {
        }

        public class Handler : HandlerWithStorage<Command, Guid>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<Guid> Handle(Command cmd, CancellationToken cancellationToken)
            {
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