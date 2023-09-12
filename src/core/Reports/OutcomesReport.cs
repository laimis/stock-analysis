using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
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
    public class OutcomesReport
    {
        public enum Duration { SingleBar, AllBars }

        public abstract class BaseQuery : RequestWithUserId<OutcomesReportView>
        {
            public BaseQuery(){}
            public BaseQuery(Duration duration, PriceFrequency frequency, bool includeGapAnalysis, Guid userId) : base(userId)
            {
                Duration = duration;
                Frequency = frequency;
                IncludeGapAnalysis = includeGapAnalysis;
            }

            [Required]
            public Duration Duration { get; set; }

            [Required]
            public PriceFrequency Frequency { get; set; }

            public bool IncludeGapAnalysis { get; set; }
            public string StartDate { get; set; }
            public string EndDate { get; set; }
        }

        public class ForTickersQuery : BaseQuery
        {
            public ForTickersQuery(){}

            [Required]
            public string[] Tickers { get; set; }
        }

        public class Handler : HandlerWithStorage<ForTickersQuery, OutcomesReportView>
        {
            public Handler(
                IAccountStorage accountStorage,
                IMarketHours marketHours,
                IPortfolioStorage storage,
                IBrokerage brokerage) : base(storage)
            {
                _accountStorage = accountStorage;
                _brokerage = brokerage;
                _marketHours = marketHours;
            }

            private IAccountStorage _accountStorage;
            private IBrokerage _brokerage { get; }
            private IMarketHours _marketHours { get; }

            public override async Task<OutcomesReportView> Handle(ForTickersQuery request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await RunAnalysis(
                    request,
                    user.State,
                    GetOutcomesFunction(request.Duration),
                    GetEvaluationFunction(request.Duration)
                );         
            }

            private static Func<PriceBar[], List<AnalysisOutcome>> GetOutcomesFunction(Duration duration) => 
                duration switch
                {
                    Duration.AllBars => (Func<PriceBar[], List<AnalysisOutcome>>)(prices => MultipleBarPriceAnalysis.Run(
                                currentPrice: prices[prices.Length - 1].Close,
                                prices
                            )),
                    Duration.SingleBar => SingleBarAnalysisRunner.Run,
                    _ => throw new ArgumentOutOfRangeException()
                };

            private static Func<List<TickerOutcomes>, IEnumerable<AnalysisOutcomeEvaluation>> GetEvaluationFunction(Duration duration) => 
                duration switch
                {
                    Duration.AllBars => (Func<List<TickerOutcomes>, IEnumerable<AnalysisOutcomeEvaluation>>)MultipleBarAnalysisOutcomeEvaluation.Evaluate,
                    Duration.SingleBar => SingleBarAnalysisOutcomeEvaluation.Evaluate,
                    _ => throw new ArgumentOutOfRangeException()
                };

            private async Task<OutcomesReportView> RunAnalysis(
                ForTickersQuery query,
                UserState user,
                Func<PriceBar[], List<AnalysisOutcome>> priceAnalysisFunc,
                Func<List<TickerOutcomes>, IEnumerable<AnalysisOutcomeEvaluation>> evaluationFunc)
            {
                var tickerOutcomes = new List<TickerOutcomes>();
                var tickerGapViews = new List<GapsView>();
                var patterns = new List<TickerPatterns>();

                foreach(var ticker in query.Tickers)
                {
                    var startDate = query.StartDate == null ? default : _marketHours.GetMarketStartOfDayTimeInUtc(DateTimeOffset.Parse(query.StartDate));
                    var endDate = query.EndDate == null ? default : _marketHours.GetMarketEndOfDayTimeInUtc(DateTimeOffset.Parse(query.EndDate));

                    var pricesResponse = await _brokerage.GetPriceHistory(user, ticker, query.Frequency, start: startDate, end: endDate);
                    
                    if (!pricesResponse.IsOk || pricesResponse.Success == null || pricesResponse.Success.Length == 0)
                    {
                        continue;
                    }

                    var prices = pricesResponse.Success;

                    var outcomes = priceAnalysisFunc(prices);

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, ticker));

                    if (query.IncludeGapAnalysis)
                    {
                        var gapOutcomes = GapAnalysis.Generate(prices, 60);
                        tickerGapViews.Add(new GapsView(gapOutcomes, ticker));
                    }

                    var tickerPatterns = new TickerPatterns(
                        PatternDetection.Generate(prices).ToList(),
                        ticker);

                    patterns.Add(tickerPatterns);
                }

                return new OutcomesReportView(
                    evaluations: evaluationFunc(tickerOutcomes),
                    outcomes: tickerOutcomes,
                    gaps: tickerGapViews,
                    patterns: patterns
                );
            }
        }
    }
}