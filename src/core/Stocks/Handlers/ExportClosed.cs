using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Shared;
using core.Stocks.View;

namespace core.Stocks
{
    public class ExportClosed
    {
        public class Query : RequestWithUserId<ExportResponse>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, ExportResponse>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<ExportResponse> Handle(Query request, CancellationToken cancellationToken)
            {
                var stocks = await _storage.GetStocks(request.UserId);

                var closedTransactions = stocks
                    .SelectMany(s => s.State.Transactions.Where(t => t.IsPL))
                    .Select(t => new StockTransactionView(t))
                    .OrderByDescending(p => p.Date)
                    .ToList();

                var filename = CSVExport.GenerateFilename("closedstocks");

                return new ExportResponse(filename, CSVExport.Generate(closedTransactions));
            }
        }
    }
}