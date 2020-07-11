using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Options
{
    public class Details
    {
        public class Query : RequestWithUserId<object>
        {
            [Required]
            public Guid Id { get; set; }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private IStocksService2 _stockService;

            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stockService) : base(storage)
            {
                _stockService = stockService;
            }

            public override async Task<object> Handle(Query request, CancellationToken cancellationToken)
            {
                var option = await _storage.GetOwnedOption(request.Id, request.UserId);

                var price = await _stockService.GetPrice(option.Ticker);
                
                return Map(option, price);
            }

            private object Map(OwnedOption o, TickerPrice currentPrice)
            {
                return new OwnedOptionSummary(o, currentPrice);
            }
        }
    }
}