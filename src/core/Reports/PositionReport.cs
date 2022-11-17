using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks;
using core.Stocks.Services.Analysis;

namespace core.Reports
{
    public class PositionReport
    {
        public class Query : RequestWithUserId<OutcomesReportView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, OutcomesReportView>
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
        
            public override async Task<OutcomesReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(request.UserId);

                var positions = stocks
                    .Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .ToList();

                return await RunAnalysis(
                    positions,
                    user.State
                );
            }

            private async Task<OutcomesReportView> RunAnalysis(
                IEnumerable<PositionInstance> positions,
                UserState user
                )
            {
                var tickerOutcomes = new List<TickerOutcomes>();
                
                foreach(var position in positions)
                {
                    var quoteResponse = await _brokerage.GetQuote(user, position.Ticker);
                    if (!quoteResponse.IsOk)
                    {
                        continue;
                    }

                    var currentPrice = Math.Max(quoteResponse.Success.bidPrice, quoteResponse.Success.lastPrice);

                    position.SetPrice(currentPrice);

                    var outcomes = PositionAnalysis.Generate(position).ToList();

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, position.Ticker));
                }

                var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(tickerOutcomes);

                return new OutcomesReportView(
                    evaluations: evaluations,
                    tickerOutcomes,
                    new List<GapsView>());
            }
        }
    }
}