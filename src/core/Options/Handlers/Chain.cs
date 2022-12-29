using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Options;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using MediatR;

namespace core.Options
{
    public class Chain
    {
        public class Query : RequestWithUserId<OptionDetailsViewModel>
        {
            public Query(string ticker, Guid userId) : base(userId)
            {
                Ticker = ticker;
            }

            public string Ticker { get; }
        }

        public class Handler : IRequestHandler<Query, OptionDetailsViewModel>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private IOptionsService _options;
            
            public Handler(IAccountStorage accounts, IBrokerage brokerage, IOptionsService options)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _options = options;
            }
            
            public async Task<OptionDetailsViewModel> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var priceResult = await _brokerage.GetQuote(user.State, request.Ticker);
                var price = priceResult.IsOk switch {
                    true => priceResult.Success.lastPrice,
                    false => (decimal?)null
                };

                var dates = await _options.GetOptions(request.Ticker);

                var upToFour = dates.Take(4);

                var options = new List<OptionDetail>();

                foreach (var d in upToFour)
                {
                    var details = await _options.GetOptionDetails(request.Ticker, d);
                    options.AddRange(details);
                }

                return MapOptionDetails(price, options);
            }
        }

        public static OptionDetailsViewModel MapOptionDetails(
            decimal? price,
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