using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
using core.Shared.Adapters.Brokerage;
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

        public class Handler : IRequestHandler<Query, object>
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

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _storage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var pricesResponse = await _brokerage.GetHistoricalPrices(user.State, request.Ticker);
                if (!pricesResponse.IsOk)
                {
                    throw new Exception("Failed to get historical prices");
                }
                var prices = pricesResponse.Success;
                
                var priceResponse = await _stocksService2.GetPrice(request.Ticker);
                if (!priceResponse.IsOk)
                {
                    throw new Exception("Failed to get price");
                }

                var price = priceResponse.Success.Amount;
                
                // find historical price with the lowest closing price
                var lowest = prices[0];
                foreach (var p in prices)
                {
                    if (p.Close < lowest.Close)
                    {
                        lowest = p;
                    }
                }

                // find historical price with the highest closing price
                var highest = prices[0];
                foreach (var p in prices)
                {
                    if (p.Close > highest.Close)
                    {
                        highest = p;
                    }
                }

                var outcomes = StockPriceAnalysis.Run(price, prices);

                return new
                {
                    Ticker = request.Ticker,
                    Price = price,
                    historicalPrices = new PricesView(prices),
                    High = highest,
                    Low = lowest,
                    Outcomes = outcomes
                };
            }
        }
    }
}