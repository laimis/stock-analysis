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
using core.Stocks.Services;
using MediatR;

namespace core.Reports
{
    public class OutcomesReport
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

        public class ForPortfolioQuery : BaseQuery
        {
            public ForPortfolioQuery(Duration duration, PriceFrequency frequency, bool includeGapAnalysis, Guid userId)
                : base(duration, frequency, includeGapAnalysis, userId)
            {
            }
        }

        public class ForTickerQuery : BaseQuery
        {
            public ForTickerQuery(Duration duration, PriceFrequency frequency, bool includeGapAnalysis, string ticker, Guid userId)
             : base(duration, frequency, includeGapAnalysis, userId)
            {
                Ticker = ticker;
            }
            public string Ticker { get; }
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

        public class Handler : HandlerWithStorage<ForPortfolioQuery, OutcomesReportView>,
            IRequestHandler<ForTickerQuery, OutcomesReportView>,
            IRequestHandler<ForTickersQuery, OutcomesReportView>
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

            public async Task<OutcomesReportView> Handle(ForTickerQuery request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await RunAnalysis(
                    request.Frequency,
                    new[] {request.Ticker},
                    user.State,
                    GetOutcomesFunction(request.Duration),
                    GetEvaluationFunction(request.Duration),
                    includeGapAnalysis: request.IncludeGapAnalysis
                );         
            }

            public async Task<OutcomesReportView> Handle(ForTickersQuery request, CancellationToken cancellationToken)
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
        
            public override async Task<OutcomesReportView> Handle(ForPortfolioQuery request, CancellationToken cancellationToken)
            {
                var user = await _accountStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(request.UserId);

                var tickers = stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.Ticker).ToList();

                var outcomesFunc = GetOutcomesFunction(request.Duration);
                var evaluationFunc = GetEvaluationFunction(request.Duration);

                return await RunAnalysis(
                    request.Frequency,
                    tickers,
                    user.State,
                    outcomesFunc,
                    evaluationFunc,
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

            private static Func<List<TickerOutcomes>, IEnumerable<OutcomeAnalysisEvaluation>> GetEvaluationFunction(Duration duration) => 
                duration switch
                {
                    Duration.AllBars => (Func<List<TickerOutcomes>, IEnumerable<OutcomeAnalysisEvaluation>>)MultipleBarAnalysisOutcomeEvaluation.Evaluate,
                    Duration.SingleBar => SingleBarAnalysisOutcomeEvaluation.Evaluate,
                    _ => throw new ArgumentOutOfRangeException()
                };

            private async Task<OutcomesReportView> RunAnalysis(
                PriceFrequency frequency,
                IEnumerable<string> tickers,
                UserState user,
                Func<PriceBar[], List<AnalysisOutcome>> priceAnalysisFunc,
                Func<List<TickerOutcomes>, IEnumerable<OutcomeAnalysisEvaluation>> evaluationFunc,
                bool includeGapAnalysis)
            {
                var ticketOutcomes = new List<TickerOutcomes>();
                var tickerGapViews = new List<GapsView>();

                foreach(var ticker in tickers)
                {
                    var priceHistoryResponse = await _brokerage.GetPriceHistory(user, ticker, frequency);
                    if (!priceHistoryResponse.IsOk || priceHistoryResponse.Success == null || priceHistoryResponse.Success.Length == 0)
                    {
                        continue;
                    }

                    var outcomes = priceAnalysisFunc(priceHistoryResponse.Success);

                    ticketOutcomes.Add(new TickerOutcomes(outcomes, ticker));

                    if (includeGapAnalysis)
                    {
                        var gapOutcomes = GapAnalysis.Generate(priceHistoryResponse.Success, 60);
                        tickerGapViews.Add(new GapsView(gapOutcomes, ticker));
                    }
                }

                return new OutcomesReportView(
                    new AnalysisReportView(
                        evaluationFunc(ticketOutcomes)
                    ),
                    ticketOutcomes,
                    tickerGapViews
                );
            }
        }
    }
}