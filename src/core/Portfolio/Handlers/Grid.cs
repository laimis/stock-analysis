using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services;

namespace core.Portfolio
{
    public class Grid
    {
        public class Query : RequestWithUserId<IEnumerable<GridEntry>>
        {
            public Query(Guid userId) : base(userId){}
        }

        public class Handler : HandlerWithStorage<Query, IEnumerable<GridEntry>>
        {
            public Handler(
                IAccountStorage accountStorage,
                IPortfolioStorage storage,
                IBrokerage brokerage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
            }

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }

            public override async Task<IEnumerable<GridEntry>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(request.UserId);

                return stocks
                    .Where(s => s.State.OpenPosition != null)
                    .Select(async s => {
                        {
                            var historicalResponse = await _brokerage.GetHistoricalPrices(user.State, s.State.Ticker);

                            var outcomes = StockPriceAnalysis.Run(
                                currentPrice: historicalResponse.Success.Last().Close,
                                historicalResponse.Success
                            );

                            return new GridEntry(s.State.Ticker, outcomes);
                        }
                    })
                    .Select(t => t.Result);
            }
        }
    }
}