using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class Lists
    {
        public class Query : RequestWithUserId<StockListState[]>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : IRequestHandler<Query, StockListState[]>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<StockListState[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var lists = await _portfolioStorage.GetStockLists(user.State.Id);
                return lists
                    .Select(x => x.State)
                    .OrderBy(x => x.Name.ToLower())
                    .ToArray();
            }
        }
    }

    public class ListsGet
    {
        public class Query : RequestWithUserId<StockListState>
        {
            public Query(string name, Guid userId) : base(userId)
            {
                Name = name;
            }

            public string Name { get; }
        }

        public class Handler : IRequestHandler<Query, StockListState>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<StockListState> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                var list = await _portfolioStorage.GetStockList(request.Name, user.State.Id);
                return list?.State;
            }
        }
    }
    
    public class ListsExport
    {
        public class Query : RequestWithUserId<ServiceResponse<ExportResponse>>
        {
            public Query(bool justTickers, string name, Guid userId) : base(userId)
            {
                JustTickers = justTickers;
                Name = name;
            }

            public bool JustTickers { get; }
            public string Name { get; }
        }

        public class Handler : HandlerWithStorage<Query, ServiceResponse<ExportResponse>>
        {
            private IAccountStorage _accountsStorage;
            private ICSVWriter _csvWriter;
            
            public Handler(
                IAccountStorage accounts,
                ICSVWriter csvWriter,
                IPortfolioStorage storage) : base(storage)
            {
                _accountsStorage = accounts;
                _csvWriter = csvWriter;
            }

            public override async Task<ServiceResponse<ExportResponse>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var list = await _storage.GetStockList(request.Name, user.State.Id);
                if (list == null)
                {
                    throw new Exception("List not found");
                }

                var filename = CSVExport.GenerateFilename($"Stocks_{request.Name}");

                var response = new ExportResponse(filename, CSVExport.Generate(_csvWriter, list.State, request.JustTickers));
                
                return new ServiceResponse<ExportResponse>(response);
            }
        }
    }
}