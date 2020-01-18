using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Options
{
    public class CloseOption
    {
        public class Command : IRequest
        {
            [Required]
            public string Ticker { get; set; }

            [Range(1, 10000)]
            public double StrikePrice { get; set; }

            [Required]
            public DateTimeOffset? Expiration { get; set; }

            [Required]
            public string OptionType { get; set; }

            [Range(1, 1000, ErrorMessage = "Invalid number of contracts specified")]
            public int Amount { get; set; }

            [Required]
            [Range(0, 1000)]
            public double? ClosePrice { get; set; }

            [Required]
            public DateTimeOffset? CloseDate { get; set; }
            
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
                var sold = await _storage.GetSoldOption(
                    cmd.Ticker,
                    cmd.Type,
                    cmd.Expiration.Value,
                    cmd.StrikePrice,
                    cmd.UserIdentifier);

                if (sold != null)
                {
                    sold.Close(cmd.Amount, cmd.ClosePrice.Value, cmd.CloseDate.Value);
                    await _storage.Save(sold);
                }

                return new Unit();
            }
        }
    }
}