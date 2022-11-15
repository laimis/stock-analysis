using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Reports.Views;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using MediatR;

namespace core.Reports
{
    public class SingleBarAnalysisReport
    {
        public class ForPortfolioQuery : RequestWithUserId<AnalysisReportView>
        {
            public ForPortfolioQuery(PriceFrequency frequency, Guid userId) : base(userId)
            {
                Frequency = frequency;
            }

            public PriceFrequency Frequency { get; }
        }

        public class ForTickerQuery: RequestWithUserId<AnalysisReportView>
        {
            public ForTickerQuery(PriceFrequency frequency, string ticker, Guid userId) : base(userId)
            {
                Frequency = frequency;
                Ticker = ticker;
            }

            public PriceFrequency Frequency { get; }
            public string Ticker { get; }
        }

        public class ForTickersQuery: RequestWithUserId<AnalysisReportView>
        {
            public ForTickersQuery(PriceFrequency frequency, string[] tickers, Guid userId) : base(userId)
            {
                Frequency = frequency;
                Tickers = tickers;
            }

            public PriceFrequency Frequency { get; }
            public string[] Tickers { get; }
        }

        public class Handler : HandlerWithStorage<ForPortfolioQuery, AnalysisReportView>,
            IRequestHandler<ForTickerQuery, AnalysisReportView>,
            IRequestHandler<ForTickersQuery, AnalysisReportView>
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
            private const decimal SigmaRatioThreshold = 1m;
            private const decimal SmallPercentChange = 2m;
            private const decimal ExcellentClosingRange = 80m;
            private const decimal LowClosingRange = 20m;

            public async Task<AnalysisReportView> Handle(ForTickerQuery request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var tickers = new [] { request.Ticker };

                return await GenerateAnalysisReport(request.Frequency, tickers, user);
            }

            public async Task<AnalysisReportView> Handle(ForTickersQuery request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await GenerateAnalysisReport(request.Frequency, request.Tickers, user);
            }

            public override async Task<AnalysisReportView> Handle(ForPortfolioQuery request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var tickers = await GetTickers(user.State);

                return await GenerateAnalysisReport(request.Frequency, tickers, user);
            }

            private async Task<AnalysisReportView> GenerateAnalysisReport(PriceFrequency frequency, IEnumerable<string> tickers, User user)
            {
                var tickerOutcomes = new List<TickerOutcomes>();
                foreach (var ticker in tickers)
                {
                    var prices = await _brokerage.GetPriceHistory(
                        user.State,
                        ticker,
                        frequency
                    );

                    if (prices.Success.Length == 0)
                    {
                        continue;
                    }

                    var outcomes = SingleBarAnalysisRunner.Run(prices.Success);

                    tickerOutcomes.Add(new TickerOutcomes(outcomes, ticker));
                }

                var categories = GenerateReportCategories(tickerOutcomes);

                return new AnalysisReportView(categories);
            }

            private async Task<List<string>> GetTickers(UserState user)
            {
                var stocks = await _storage.GetStocks(user.Id);

                return stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition.Ticker)
                    .ToList();
            }

            private static IEnumerable<AnalysisCategoryGrouping> GenerateReportCategories(List<TickerOutcomes> tickerOutcomes)
            {
                // stocks that had above average volume grouping
                yield return new Views.AnalysisCategoryGrouping(
                    "Above Average Volume and High Percent Change",
                    OutcomeType.Positive,
                    SingleBarOutcomeKeys.RelativeVolume,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    SingleBarOutcomeKeys.ClosingRange,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold)
                        ).ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "High Volume with Excellent Closing Range and High Percent Change",
                    OutcomeType.Positive,
                    SingleBarOutcomeKeys.RelativeVolume,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value >= ExcellentClosingRange)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value >= SigmaRatioThreshold)
                        ).ToList()
                );

                // negative outcome types
                yield return new Views.AnalysisCategoryGrouping(
                    "Above Average Volume and Negative Percent Change",
                    OutcomeType.Negative,
                    SingleBarOutcomeKeys.RelativeVolume,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SigmaRatio && o.value < -1 * SigmaRatioThreshold))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "Low Closing Range",
                    OutcomeType.Negative,
                    SingleBarOutcomeKeys.ClosingRange,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.ClosingRange && o.value < LowClosingRange))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "Above Average Volume but Small Positive Percent Change",
                    OutcomeType.Negative,
                    SingleBarOutcomeKeys.RelativeVolume,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.RelativeVolume && o.value >= RelativeVolumeThresholdPositive)
                            && t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.PercentChange && o.value >= 0 && o.value < SmallPercentChange))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "SMA20 Below SMA50 Recent",
                    OutcomeType.Neutral,
                    SingleBarOutcomeKeys.SMA20Above50Days,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.SMA20Above50Days && o.value <= 0 && o.value > -5))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "Positive gap ups",
                    OutcomeType.Positive,
                    SingleBarOutcomeKeys.GapPercentage,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value > 0))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "Negative gap downs",
                    OutcomeType.Negative,
                    SingleBarOutcomeKeys.GapPercentage,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.GapPercentage && o.value < 0))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "New Highs",
                    OutcomeType.Positive,
                    SingleBarOutcomeKeys.NewHigh,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.NewHigh && o.value > 0))
                        .ToList()
                );

                yield return new Views.AnalysisCategoryGrouping(
                    "New Lows",
                    OutcomeType.Negative,
                    SingleBarOutcomeKeys.NewLow,
                    tickerOutcomes
                        .Where(t =>
                            t.Outcomes.Any(o => o.key == SingleBarOutcomeKeys.NewLow && o.value < 0))
                        .ToList()
                );
            }
        }
    }
}