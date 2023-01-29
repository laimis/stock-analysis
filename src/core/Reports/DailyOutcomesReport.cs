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
            public Query(string start, string end, string ticker, Guid userId) : base(userId)
            {
                Start = start;
                End = end;
                Ticker = ticker;
            }

            public string Start { get; }
            public string End { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Query, DailyOutcomesReportView>
        {
            public Handler(
                IAccountStorage accountStorage,
                IBrokerage brokerage,
                IMarketHours marketHours,
                IPortfolioStorage storage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
                _marketHours = marketHours;
            }

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }
            private IMarketHours _marketHours;

            public override async Task<DailyOutcomesReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var start = _marketHours.GetMarketStartOfDayTimeInUtc(
                    DateTimeOffset.Parse(request.Start)
                );

                var end = request.End == null ? default : 
                    _marketHours.GetMarketEndOfDayTimeInUtc(
                        DateTimeOffset.Parse(request.End)
                    );

                var priceResponse = await _brokerage.GetPriceHistory(
                    user.State,
                    request.Ticker,
                    frequency: PriceFrequency.Daily,
                    start: start.AddDays(-200), // go back a bit to have enough data for 'relative' stats
                    end: end
                );
                if (!priceResponse.IsOk)
                {
                    throw new Exception("Failed to get price history: " + priceResponse.Error.Message);
                }

                var bars = priceResponse.Success;

                var indexOfFirtsBar = 0;
                foreach(var bar in bars)
                {
                    if (bar.Date.Date == start.Date)
                    {
                        break;
                    }

                    indexOfFirtsBar++;
                }

                var scoreList = Enumerable.Range(indexOfFirtsBar, bars.Length - indexOfFirtsBar)
                    .Select(index => {
                        var currentBar = bars[index];
                        var previousBars = bars[..index];
                        
                        var outcomes = SingleBarAnalysisRunner.Run(currentBar, previousBars);
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