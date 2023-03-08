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

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }

            private ISECFilings _secFilings;

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
                var tickerPatterns = new List<TickerPatterns>();
                var filings = new Dictionary<string, Shared.Adapters.SEC.CompanyFilings>();
                
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

                    // TODO: for some reason SEC filings from DO are SUPER slow
                    // var tickerFilings = await _secFilings.GetFilings(position.Ticker);
                    // if (tickerFilings.IsOk)
                    // {
                    //     filings.Add(position.Ticker, tickerFilings.Success);
                    // } 

                    var outcomes = PositionAnalysis.Generate(
                        position,
                        bars).ToList();

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, position.Ticker));

                    var patterns = PatternDetection.Generate(bars).ToList();
                    
                    tickerPatterns.Add(new TickerPatterns(patterns, position.Ticker));
                }

                var evaluations = PositionAnalysisOutcomeEvaluation.Evaluate(
                    tickerOutcomes,
                    filings
                );

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