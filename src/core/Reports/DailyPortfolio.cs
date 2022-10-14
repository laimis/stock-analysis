using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Portfolio;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.Services;

namespace core.Reports
{
    public class DailyPortfolio
    {
        public class Query : RequestWithUserId<DailyPortfolioReportView>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, DailyPortfolioReportView>
        {
            private IAccountStorage _accounts;
            private IStocksService2 _stockService;
            private IBrokerage _brokerage;

            public Handler(IBrokerage brokerage, IAccountStorage accounts, IPortfolioStorage storage, IStocksService2 stocksService) : base(storage)
            {
                _accounts = accounts;
                _stockService = stocksService;
                _brokerage = brokerage;
            }

            // report params
            private const decimal RelativeVolumeThresholdPositive = 0.9m;
            private const decimal HighPercentChange = 5m;
            private const decimal SmallPercentChange = 2m;
            private const decimal ExcellentClosingRange = 0.8m;
            private const decimal LowClosingRange = 0.2m;

            public override async Task<DailyPortfolioReportView> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .ToList();

                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var tickerOutcomes = new List<PositionAnalysisEntry>();
                foreach (var p in positions)
                {
                    var prices = await _brokerage.GetHistoricalPrices(
                        user.State,
                        p.Ticker
                    );

                    var dailyOutcomes = DailyPriceAnalysisRunner.Run(prices.Success);

                    tickerOutcomes.Add(new PositionAnalysisEntry(p, dailyOutcomes));
                }

                var categories = GenerateReportCategories(tickerOutcomes);

                return new DailyPortfolioReportView(categories);
            }

            private static IEnumerable<DailyPortfolioReportCategory> GenerateReportCategories(List<PositionAnalysisEntry> tickerOutcomes)
            {
                // stocks that had above average volume grouping
                yield return new Views.DailyPortfolioReportCategory(
                    "Above Average Volume and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.PercentChange && o.value >= HighPercentChange))
                        .ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "Positive Closes",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t => t.Outcomes.Any(o =>
                            o.key == DailyOutcomeKeys.PercentChange && o.value >= 0))
                        .ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.PercentChange && o.value >= HighPercentChange)
                        ).ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "High Volume with Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.PercentChange && o.value >= HighPercentChange)
                        ).ToList()
                );

                // negative outcome types
                yield return new Views.DailyPortfolioReportCategory(
                    "Above Average Volume and Negative Percent Change",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.PercentChange && o.value < -1 * HighPercentChange))
                        .ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "Low Closing Range",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.ClosingRange && o.value < LowClosingRange))
                        .ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "Above Average Volume but Small Percent Change",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == DailyOutcomeKeys.PercentChange && o.value < SmallPercentChange))
                        .ToList()
                );

                yield return new Views.DailyPortfolioReportCategory(
                    "SMA20 Below SMA50 Recent",
                    OutcomeType.Neutral,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == DailyOutcomeKeys.SMA20Above50Days && o.value <= 0 && o.value > -5))
                        .ToList()
                );
            }
        }
    }
}