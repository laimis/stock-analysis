using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
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
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private ICSVWriter _csvWriter;
            
            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                ICSVWriter csvWriter,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _csvWriter = csvWriter;
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

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
                    ExportType.Open => 
                        await _brokerage.GetQuotes(user.State, final.Select(p => p.Ticker)),
                    _ => new ServiceResponse<System.Collections.Generic.Dictionary<string, StockQuote>>(
                        new System.Collections.Generic.Dictionary<string, StockQuote>()
                    )
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