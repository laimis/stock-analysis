﻿using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
{
    public class Details
    {
        public class Query : IRequest<object>
        {
            public int? EmaPeriod { get; }
            public string Ticker { get; }

            public Query(int? emaPeriod, string ticker)
            {
                EmaPeriod = emaPeriod;
                Ticker = ticker;
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
                var emaPrice = request.EmaPeriod.HasValue switch {
                    true => _stocksService2.GetEmaPrice(request.Ticker, request.EmaPeriod.Value),
                    false => Task.FromResult<StockServiceResponse<Price?>>(
                        new StockServiceResponse<Price?>((Price?)null)
                    )
                };

                await Task.WhenAll(profile, advanced, price, emaPrice);
                
                return new StockDetailsView
                {
                    Ticker = request.Ticker,
                    Price = price.Result.Success.Amount,
                    Profile = profile.Result.Success,
                    Stats = advanced.Result.Success,
                    EmaPrice = emaPrice.Result.Success?.Amount
                };
            }
        }
    }
}