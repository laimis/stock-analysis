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

namespace core.Reports
{
    public class PercentChangeStatistics
    {
        public class ForTickerQuery: RequestWithUserId<PercentChangeStatisticsView>
        {
            public ForTickerQuery(PriceFrequency frequency, string ticker, Guid userId) : base(userId)
            {
                Frequency = frequency;
                Ticker = ticker;
            }

            public PriceFrequency Frequency { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<ForTickerQuery, PercentChangeStatisticsView>
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

            public async override Task<PercentChangeStatisticsView> Handle(ForTickerQuery request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                return await Generate(request.Frequency, request.Ticker, user);
            }

            private async Task<PercentChangeStatisticsView> Generate(PriceFrequency frequency, string ticker, User user)
            {
                var pricesResponse = await _brokerage.GetHistoricalPrices(
                    user.State,
                    ticker,
                    frequency
                );

                if (!pricesResponse.IsOk)
                {
                    throw new Exception("Failed to get prices");
                }

                var prices = pricesResponse.Success;

                return new PercentChangeStatisticsView(
                    ticker,
                    recent: PercentChangeAnalysis.Generate(prices.Skip(prices.Length - 60).ToArray()),
                    allTime: PercentChangeAnalysis.Generate(prices)
                );
            }
        }
    }
}