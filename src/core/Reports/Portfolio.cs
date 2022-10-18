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
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;

namespace core.Reports
{
    public class Portfolio
    {
        public class Query : RequestWithUserId<PortfolioReportView>
        {
            public Query(PriceFrequency frequey, Guid userId) : base(userId)
            {
                Frequency = frequey;
            }

            public static Query Daily(Guid userId) => new(PriceFrequency.Daily, userId);
            public static Query Weekly(Guid userId) => new(PriceFrequency.Weekly, userId);

            public PriceFrequency Frequency { get; }
        }

        public class Handler : HandlerWithStorage<Query, PortfolioReportView>
        {
            private IAccountStorage _accounts;
            private IStocksService2 _stockService;
            private IBrokerage _brokerage;

            public Handler(
                IBrokerage brokerage,
                IAccountStorage accounts,
                IPortfolioStorage storage,
                IStocksService2 stocksService) : base(storage)
            {
                _accounts = accounts;
                _stockService = stocksService;
                _brokerage = brokerage;
            }

            // report params
            private const decimal RelativeVolumeThresholdPositive = 0.9m;
            private const decimal HighPercentChange = 5m;
            private const decimal SmallPercentChange = 2m;
            private const decimal ExcellentClosingRange = 80m;
            private const decimal LowClosingRange = 20m;

            public override async Task<PortfolioReportView> Handle(Query request, CancellationToken cancellationToken)
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
                        p.Ticker,
                        request.Frequency
                    );

                    if (prices.Success.Length == 0)
                    {
                        continue;
                    }

                    var outcomes = SingleBarAnalysisRunner.Run(prices.Success);

                    tickerOutcomes.Add(new PositionAnalysisEntry(p, outcomes));
                }

                var categories = GenerateReportCategories(tickerOutcomes);

                return new PortfolioReportView(categories);
            }

            private static IEnumerable<PortfolioReportCategory> GenerateReportCategories(List<PositionAnalysisEntry> tickerOutcomes)
            {
                // stocks that had above average volume grouping
                yield return new Views.PortfolioReportCategory(
                    "Above Average Volume and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= HighPercentChange))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Positive Closes",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t => t.Outcomes.Any(o =>
                            o.key == SingleBarOutcomeKeys.PercentChange && o.value >= 0))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= HighPercentChange)
                        ).ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "High Volume with Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= HighPercentChange)
                        ).ToList()
                );

                // negative outcome types
                yield return new Views.PortfolioReportCategory(
                    "Above Average Volume and Negative Percent Change",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value < -1 * HighPercentChange))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Low Closing Range",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value < LowClosingRange))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Above Average Volume but Small Positive Percent Change",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= 0 && o.value < SmallPercentChange))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "SMA20 Below SMA50 Recent",
                    OutcomeType.Neutral,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SMA20Above50Days && o.value <= 0 && o.value > -5))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Positive gap ups",
                    OutcomeType.Positive,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value > 0))
                        .ToList()
                );

                yield return new Views.PortfolioReportCategory(
                    "Negative gap downs",
                    OutcomeType.Negative,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value < 0))
                        .ToList()
                );
            }
        }
    }
}