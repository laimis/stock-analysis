using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Utils;
using MediatR;

namespace core.Options
{
    public class Open
    {
        public class Command : RequestWithUserId<Guid>
        {
            [Required]
            public string Ticker { get; set; }

            [Range(1, 10000)]
            public double StrikePrice { get; set; }

            [Required]
            public DateTimeOffset? ExpirationDate { get; set; }

            [Required]
            [ValidValues(nameof(core.Options.OptionType.CALL), nameof(core.Options.OptionType.PUT))]
            public string OptionType { get; set; }

            [Required]
            [ValidValues(nameof(core.Options.PositionType.Buy), nameof(core.Options.PositionType.Sell))]
            public string PositionType { get; set; }

            [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
            public int Amount { get; set; }

            [Range(1, 1000)]
            public double Premium { get; set; }

            [Required]
            public DateTimeOffset? Filled { get; set; }
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
                var positionType = (PositionType)Enum.Parse(typeof(PositionType), cmd.PositionType);

                var option = new OwnedOption(
                        cmd.Ticker,
                        positionType,
                        optionType,
                        cmd.ExpirationDate.Value,
                        cmd.StrikePrice,
                        cmd.UserId,
                        cmd.Amount,
                        cmd.Premium,
                        cmd.Filled.Value);

                await this._storage.Save(option, cmd.UserId);

                return option.State.Id;
            }
        }
    }
}