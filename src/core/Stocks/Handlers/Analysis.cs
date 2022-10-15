using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using core.Stocks.Services;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
{
    public class Analysis
    {
        public class Query : IRequest<object>
        {
            public string Ticker { get; }
            public Guid UserId { get; }

            public Query(string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class DailyQuery : IRequest<object>
        {
            public string Ticker { get; }
            public Guid UserId { get; }

            public DailyQuery(string ticker, Guid userId)
            {
                Ticker = ticker;
                UserId = userId;
            }
        }

        public class Handler : IRequestHandler<Query, object>,
            IRequestHandler<DailyQuery, object>
        {
            private IBrokerage _brokerage;
            private IStocksService2 _stocksService2;
            private IAccountStorage _storage;

            public Handler(IBrokerage brokerage, IStocksService2 stockService2, IAccountStorage storage)
            {
                _brokerage = brokerage;
                _stocksService2 = stockService2;
                _storage = storage;
            }

            public Task<object> Handle(Query request, CancellationToken cancellationToken)
             => RunAnalysis(
                    request.UserId,
                    request.Ticker, 
                    prices => HistoricalPriceAnalysis.Run(prices[prices.Length - 1].Close, prices)
                );

            public Task<object> Handle(DailyQuery request, CancellationToken cancellationToken)
                => RunAnalysis(
                    request.UserId,
                    request.Ticker,
                    prices => LatestBarAnalysisRunner.Run(prices)
                );

            private async Task<object> RunAnalysis(Guid userId, string ticker, Func<HistoricalPrice[], List<AnalysisOutcome>> func)
            {
                var user = await _storage.GetUser(userId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var pricesResponse = await _brokerage.GetHistoricalPrices(user.State, ticker);
                if (!pricesResponse.IsOk)
                {
                    throw new Exception("Failed to get historical prices");
                }
                var prices = pricesResponse.Success;

                var outcomes = func(prices);

                return new
                {
                    Ticker = ticker,
                    Price = prices[prices.Length - 1].Close,
                    historicalPrices = new PricesView(prices),
                    Outcomes = outcomes
                };
            }
        }
    }
}