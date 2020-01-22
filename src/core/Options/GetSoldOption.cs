using System;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;

namespace core.Options
{
    public class GetSoldOption
    {
        public class Query : RequestWithUserId<object>
        {
            public string Ticker { get; set; }
            public string Type { get; set; }
            public double StrikePrice { get; set; }
            public DateTimeOffset Expiration { get; set; }
            public OptionType OptionType => (OptionType)Enum.Parse(typeof(OptionType), this.Type);
        }

        public class Handler : HandlerWithStorage<Query, object>
        {

            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var sold = await _storage.GetSoldOption(
                    request.Ticker,
                    request.OptionType,
                    request.Expiration,
                    request.StrikePrice,
                    request.UserId);
                
                return Mapper.ToOptionView(sold);
            }
        }
    }
}