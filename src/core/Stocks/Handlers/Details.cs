﻿using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using MediatR;

namespace core.Stocks
{
    public class Details
    {
        public class Query : IRequest<object>
        {
            public string Ticker { get; private set; }

            public Query(string ticker)
            {
                this.Ticker = ticker;
            }
        }

        public class Handler : IRequestHandler<Query, object>
        {
            private IStocksService2 _stocksService2;

            public Handler(IStocksService2 stockService2)
            {
                _stocksService2 = stockService2;
            }

            public async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var profile = _stocksService2.GetCompanyProfile(request.Ticker);
                var advanced = _stocksService2.GetAdvancedStats(request.Ticker);
                var price = _stocksService2.GetPrice(request.Ticker);
                
                await Task.WhenAll(profile, advanced, price);
                
                return Mapper.MapStockDetail(
                    request.Ticker,
                    price.Result.Amount,
                    profile.Result,
                    advanced.Result);
            }
        }
    }
}