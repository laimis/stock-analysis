using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Utils;
using MediatR;

namespace core.Options
{
    public class SellOption
    {
        public class Command : IRequest
        {
            [Required]
            public string Ticker { get; set; }

            [Range(1, double.MaxValue)]
            public double StrikePrice { get; set; }

            [Required]
            [NotInThePast]
            public DateTimeOffset? ExpirationDate { get; set; }

            [Required]
            public string OptionType { get; set; }

            [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
            public int Amount { get; set; }

            [Range(1, 1000)]
            public double Premium { get; set; }

            [Required]
            public DateTimeOffset? Filled { get; set; }
            public string UserIdentifier { get; private set; }

            public void WithUser(string userId)
            {
                this.UserIdentifier = userId;
            }

            public OptionType Type => (OptionType)Enum.Parse(typeof(OptionType), this.OptionType);
        }

        public class Handler : IRequestHandler<Command>
        {
            private IPortfolioStorage _storage;

            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }
            
            public async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var option = await this._storage.GetSoldOption(cmd.Ticker, cmd.Type, cmd.ExpirationDate.Value, cmd.StrikePrice, cmd.UserIdentifier);
                if (option == null)
                {
                    option = new SoldOption(cmd.Ticker, cmd.Type, cmd.ExpirationDate.Value, cmd.StrikePrice, cmd.UserIdentifier);
                }

                option.Open(cmd.Amount, cmd.Premium, cmd.Filled.Value);

                await this._storage.Save(option);

                return new Unit();
            }
        }
    }
}