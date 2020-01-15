using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Options
{
    public class GetSoldOption
    {
        public class Query : IRequest<object>
        {
            public string Ticker { get; set; }
            public string Type { get; set; }
            public double StrikePrice { get; set; }
            public DateTimeOffset Expiration { get; set; }
            public string UserId { get; set; }
            public OptionType OptionType => (OptionType)Enum.Parse(typeof(OptionType), this.Type);
        }

        public class Handler : IRequestHandler<Query, object>
        {

            private IPortfolioStorage _storage;
            
            public Handler(IPortfolioStorage storage)
            {
                _storage = storage;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var sold = await _storage.GetSoldOption(
                    request.Ticker,
                    request.OptionType,
                    request.Expiration,
                    request.StrikePrice,
                    request.UserId);
                
                return ToOptionView(sold);
            }

            public static object ToOptionView(SoldOption o)
            {
                return new
                {
                    ticker = o.State.Ticker,
                    type = o.State.Type.ToString(),
                    strikePrice = o.State.StrikePrice,
                    expiration = o.State.Expiration.ToString("yyyy-MM-dd"),
                    premium = o.State.Premium,
                    amount = o.State.Amount,
                    riskPct = o.State.Premium / (o.State.StrikePrice * 100) * 100,
                    profit = o.State.Profit
                };
            }
        }
    }
}