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
using core.Stocks.Services;

namespace core.Reports
{
    public class PositionReport
    {
        public class Query : RequestWithUserId<AnalysisReportView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, AnalysisReportView>
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
        
            public override async Task<AnalysisReportView> Handle(Query request, CancellationToken cancellationToken)
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

            private async Task<AnalysisReportView> RunAnalysis(
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

                var categories = GenerateReportCategories(tickerOutcomes);

                return new AnalysisReportView(categories);
            }


            private const decimal PercentToStopThreshold = -0.02m;

            private IEnumerable<AnalysisCategoryGrouping> GenerateReportCategories(List<TickerOutcomes> tickerOutcomes)
            {
                // stocks whose stops are close
                yield return new Views.AnalysisCategoryGrouping(
                    "Stop loss at risk",
                    OutcomeType.Negative,
                    PortfolioAnalysisKeys.StopLossAtRisk,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.StopLossAtRisk && o.value >= PercentToStopThreshold))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "R1 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R1Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R1Achieved && o.value > 0) &&
                            !t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R2Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "R2 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R2Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R2Achieved && o.value > 0) &&
                            !t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R3Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "R3 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R3Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R3Achieved && o.value > 0) &&
                            !t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R4Achieved && o.value > 0)
                        ).ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "R4 achieved",
                    OutcomeType.Positive,
                    PortfolioAnalysisKeys.R4Achieved,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == PortfolioAnalysisKeys.R4Achieved && o.value > 0)
                        ).ToList()
                );
            }
        }
    }
}