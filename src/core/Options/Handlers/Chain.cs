using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Options;
using MediatR;

namespace core.Options
{
    public class Chain
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

                return MapOptionDetails(price.Amount, options);
            }
        }

        public static OptionDetailsViewModel MapOptionDetails(
            decimal price,
            IEnumerable<OptionDetail> options)
        {
            var optionList = options
                .Where(o => o.Volume > 0 || o.OpenInterest > 0)
                .Where(o => o.ParsedExpirationDate > DateTime.UtcNow)
                .OrderBy(o => o.ExpirationDate)
                .ToArray();

            var expirations = optionList.Select(o => o.ExpirationDate)
                .Distinct()
                .OrderBy(s => s)
                .ToArray();

            var puts = optionList.Where(o => o.IsPut);
            var calls = optionList.Where(o => o.IsCall);

            return new OptionDetailsViewModel
            {
                StockPrice = price,
                Options = optionList,
                Expirations = expirations,
                LastUpdated = optionList.Max(o => o.LastUpdated),
                Breakdown = new OptionBreakdownViewModel
                {
                    CallVolume = calls.Sum(o => o.Volume),
                    CallSpend = calls.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnCalls = calls.Average(o => o.Volume * o.StrikePrice) / calls.Average(o => o.Volume),

                    PutVolume = puts.Sum(o => o.Volume),
                    PutSpend = puts.Sum(o => o.Volume * o.Bid),
                    PriceBasedOnPuts = puts.Average(o => o.Volume * o.StrikePrice) / puts.Average(o => o.Volume),
                }
            };
        }
    }
}