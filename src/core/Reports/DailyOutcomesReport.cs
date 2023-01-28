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
using core.Stocks.Services.Analysis;

namespace core.Reports
{
    public class DailyOutcomesReport
    {
        public class Query : RequestWithUserId<DailyOutcomesReportView>
        {
            public Query(DateTimeOffset start, DateTimeOffset? end, string ticker, Guid userId) : base(userId)
            {
                Start = start;
                End = end ?? default;
                Ticker = ticker;
            }

            public DateTimeOffset Start { get; }
            public DateTimeOffset End { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Query, DailyOutcomesReportView>
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
        
            public override async Task<DailyOutcomesReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var start = request.Start.AddDays(-200);

                var priceResponse = await _brokerage.GetPriceHistory(
                    user.State,
                    request.Ticker,
                    frequency: PriceFrequency.Daily,
                    start: start,
                    end: request.End
                );
                if (!priceResponse.IsOk)
                {
                    throw new Exception("Failed to get price history: " + priceResponse.Error.Message);
                }

                var bars = priceResponse.Success;

                var indexOfFirtsBar = 0;
                foreach(var bar in bars)
                {
                    if (bar.Date >= request.Start)
                    {
                        break;
                    }

                    indexOfFirtsBar++;
                }

                var scoreList = Enumerable.Range(indexOfFirtsBar, bars.Length - indexOfFirtsBar)
                    .Select(index => {
                        var currentBar = bars[index];
                        var outcomes = SingleBarAnalysisRunner.Run(currentBar, bars[..(index-1)]);
                        var tickerOutcomes = new TickerOutcomes(outcomes, request.Ticker);
                        var evaluations = SingleBarAnalysisOutcomeEvaluation.Evaluate(new[]{tickerOutcomes});
                        var counts = OutcomesReportView.GenerateEvaluationSummary(evaluations);
                        return new Views.DateScorePair(currentBar.Date, counts.GetValueOrDefault(request.Ticker, 0));
                    })
                    .ToList();

                return new DailyOutcomesReportView(scoreList, request.Ticker);
            }
        }
    }
}