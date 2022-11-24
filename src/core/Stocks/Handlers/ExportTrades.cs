using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;
using core.Shared.Adapters.CSV;

namespace core.Stocks
{
    public class ExportTrades
    {
        public enum ExportType
        {
            Open,
            Closed
        }

        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId, ExportType exportType) : base(userId)
            {
                ExportType = exportType;
            }

            public ExportType ExportType { get; }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            private ICSVWriter _csvWriter;
            private IStocksService2 _stockService;

            public Handler(
                ICSVWriter csvWriter,
                IStocksService2 stocksService,
                IPortfolioStorage storage) : base(storage)
            {
                _csvWriter = csvWriter;
                _stockService = stocksService;
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var trades = request.ExportType switch {
                    ExportType.Open => stocks.Where(s => s.State.OpenPosition != null).Select(s => s.State.OpenPosition),
                    ExportType.Closed => stocks.SelectMany(s => s.State.Positions).Where(p => p.IsClosed),
                    _ => throw new NotImplementedException()
                };

                var final = trades
                    .OrderByDescending(p => p.Closed ?? p.Opened)
                    .ToList();

                var prices = request.ExportType switch {
                    ExportType.Open => await _stockService.GetPrices(final.Select(p => p.Ticker)),
                    _ => new ServiceResponse<System.Collections.Generic.Dictionary<string, BatchStockPrice>>()
                };

                foreach(var p in final)
                {
                    if (prices.Success.TryGetValue(p.Ticker, out var price))
                    {
                        p.SetPrice(price.Price);
                    }
                }

                var filename = CSVExport.GenerateFilename("positions");

                return new ExportResponse(filename, CSVExport.Generate(_csvWriter, final));
            }
        }
    }
}