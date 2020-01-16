using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Options;
using MediatR;

namespace core.Options
{
    public class GetOptionDetails
    {
        public class Query : IRequest<OptionDetailsViewModel>
        {
            public Query(string ticker)
            {
                this.Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public class Handler : IRequestHandler<Query, OptionDetailsViewModel>
        {
            private IOptionsService _options;

            public Handler(IOptionsService options)
            {
                _options = options;
            }
            public async Task<OptionDetailsViewModel> Handle(Query request, CancellationToken cancellationToken)
            {
                var price = await _options.GetPrice(request.Ticker);
                if (price.NotFound)
                {
                    return null;
                }

                var dates = await _options.GetOptions(request.Ticker);

                var upToFour = dates.Take(4);

                var options = new List<OptionDetail>();

                foreach (var d in upToFour)
                {
                    var details = await _options.GetOptionDetails(request.Ticker, d);
                    options.AddRange(details);
                }

                return Mapper.MapOptionDetails(price.Amount, options);
            }
        }
    }
}