using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Shared.Adapters.Storage;
using core.Stocks.Services.Analysis;

namespace core.Reports
{
    public class GapReport
    {
        public class ForTickerQuery: RequestWithUserId<GapsView>
        {
            public ForTickerQuery(PriceFrequency frequency, string ticker, Guid userId) : base(userId)
            {
                Frequency = frequency;
                Ticker = ticker;
            }

            public PriceFrequency Frequency { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<ForTickerQuery, GapsView>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;

            public Handler(
                IBrokerage brokerage,
                IAccountStorage accounts,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public async override Task<GapsView> Handle(ForTickerQuery request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await Generate(request.Frequency, request.Ticker, user);
            }

            private async Task<GapsView> Generate(PriceFrequency frequency, string ticker, User user)
            {
                var pricesResponse = await _brokerage.GetPriceHistory(
                    user.State,
                    ticker,
                    frequency
                );

                if (!pricesResponse.IsOk)
                {
                    throw new Exception("Failed to get prices");
                }

                var prices = pricesResponse.Success;

                var gaps = GapAnalysis.Generate(prices, 60);

                return new GapsView(gaps, ticker);
            }
        }
    }
}