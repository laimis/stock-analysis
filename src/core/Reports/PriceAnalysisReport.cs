using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;

namespace core.Reports
{
    public class PriceAnalysisReport
    {
        public enum Duration { SingleBar, AllBars }

        public abstract class BaseQuery : RequestWithUserId<OutcomesReportView>
        {
            public BaseQuery(Duration duration, PriceFrequency frequency, bool includeGapAnalysis, Guid userId) : base(userId)
            {
                Duration = duration;
                Frequency = frequency;
                IncludeGapAnalysis = includeGapAnalysis;
            }

            public Duration Duration { get; }
            public PriceFrequency Frequency { get; }
            public bool IncludeGapAnalysis { get; }
        }

        public class ForTickersQuery : BaseQuery
        {
            public ForTickersQuery(Duration duration, PriceFrequency frequency, bool includeGapAnalysis, string[] tickers, Guid userId)
                : base(duration, frequency, includeGapAnalysis, userId)
            {
                Tickers = tickers;
            }

            public string[] Tickers { get; }
        }

        public class Handler : HandlerWithStorage<ForTickersQuery, OutcomesReportView>
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

            public override async Task<OutcomesReportView> Handle(ForTickersQuery request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await RunAnalysis(
                    request.Frequency,
                    request.Tickers,
                    user.State,
                    GetOutcomesFunction(request.Duration),
                    GetEvaluationFunction(request.Duration),
                    includeGapAnalysis: request.IncludeGapAnalysis
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
                PriceFrequency frequency,
                IEnumerable<string> tickers,
                UserState user,
                Func<PriceBar[], List<AnalysisOutcome>> priceAnalysisFunc,
                Func<List<TickerOutcomes>, IEnumerable<AnalysisOutcomeEvaluation>> evaluationFunc,
                bool includeGapAnalysis)
            {
                var tickerOutcomes = new List<TickerOutcomes>();
                var tickerGapViews = new List<GapsView>();

                foreach(var ticker in tickers)
                {
                    var priceHistoryResponse = await _brokerage.GetPriceHistory(user, ticker, frequency);
                    if (!priceHistoryResponse.IsOk || priceHistoryResponse.Success == null || priceHistoryResponse.Success.Length == 0)
                    {
                        continue;
                    }

                    var outcomes = priceAnalysisFunc(priceHistoryResponse.Success);

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, ticker));

                    if (includeGapAnalysis)
                    {
                        var gapOutcomes = GapAnalysis.Generate(priceHistoryResponse.Success, 60);
                        tickerGapViews.Add(new GapsView(gapOutcomes, ticker));
                    }
                }

                return new OutcomesReportView(
                    evaluations: evaluationFunc(tickerOutcomes),
                    outcomes: tickerOutcomes,
                    tickerGapViews
                );
            }
        }
    }
}