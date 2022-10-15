using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using MediatR;

namespace core.Portfolio
{
    public class Analysis
    {
        public class Query : RequestWithUserId<IEnumerable<PositionAnalysisEntry>>
        {
            public Query(Guid userId) : base(userId){}
        }

        public class DailyQuery : RequestWithUserId<IEnumerable<PositionAnalysisEntry>>
        {
            public DailyQuery(Guid userId) : base(userId){}
        }

        public class Handler : HandlerWithStorage<Query, IEnumerable<PositionAnalysisEntry>>,
            IRequestHandler<DailyQuery, IEnumerable<PositionAnalysisEntry>>
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

            public override Task<IEnumerable<PositionAnalysisEntry>> Handle(Query request, CancellationToken cancellationToken) =>
                RunAnalysis(
                    request.UserId,
                    prices => HistoricalPriceAnalysis.Run(
                                currentPrice: prices[prices.Length - 1].Close,
                                prices
                            )
                );

            public Task<IEnumerable<PositionAnalysisEntry>> Handle(DailyQuery request, CancellationToken cancellationToken) =>
                RunAnalysis(
                    request.UserId,
                    prices => LatestBarAnalysisRunner.Run(prices)
                );

            private async Task<IEnumerable<PositionAnalysisEntry>> RunAnalysis(Guid userId, Func<HistoricalPrice[], List<AnalysisOutcome>> func)
            {
                var user = await _accountStorage.GetUser(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(userId);

                return stocks
                    .Where(s => s.State.OpenPosition != null)
                    .Select(async s =>
                    {
                        {
                            var historicalResponse = await _brokerage.GetHistoricalPrices(user.State, s.State.Ticker);

                            var outcomes = func(historicalResponse.Success);

                            return new PositionAnalysisEntry(s.State.OpenPosition, outcomes);
                        }
                    })
                    .Select(t => t.Result);
            }
        }
    }
}