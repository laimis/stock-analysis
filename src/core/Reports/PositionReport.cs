using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.SEC;
using core.Shared.Adapters.Stocks;
using core.Shared.Adapters.Storage;
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
                IBrokerage brokerage,
                ISECFilings secFilings) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
                _secFilings = secFilings;
            }

            private readonly IAccountStorage _accountStorage;
            private readonly IBrokerage _brokerage;
            private readonly ISECFilings _secFilings;

            public override async Task<OutcomesReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId)
                    ?? throw new Exception("User not found");
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
                var tickerPatterns = new List<TickerPatterns>();
                
                foreach(var position in positions)
                {
                    var priceResponse = await _brokerage.GetPriceHistory(
                        user,
                        position.Ticker,
                        frequency: PriceFrequency.Daily,
                        start: position.Opened.Value,
                        end: position.Closed.HasValue ? position.Closed.Value : DateTimeOffset.UtcNow
                    );

                    if (!priceResponse.IsOk)
                    {
                        continue;
                    }

                    var bars = priceResponse.Success;

                    position.SetPrice(bars[^1].Close);

                    var orders = await _brokerage.GetAccount(user); 

                    var outcomes = PositionAnalysis.Generate(
                        position,
                        bars,
                        orders.IsOk ? orders.Success.Orders : new Order[0]).ToList();

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, position.Ticker));

                    var patterns = PatternDetection.Generate(bars).ToList();
                    
                    tickerPatterns.Add(new TickerPatterns(patterns, position.Ticker));
                }

                var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(tickerOutcomes);

                return new OutcomesReportView(
                    evaluations: evaluations,
                    tickerOutcomes,
                    new List<GapsView>(),
                    tickerPatterns
                );
            }
        }
    }
}