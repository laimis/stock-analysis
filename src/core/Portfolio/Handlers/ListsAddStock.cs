using System;
using System.ComponentModel.DataAnnotations;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Storage;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class ListsAddStock
    {
        public class Command : RequestWithUserId<StockListState>
        {
            [Required]
            public string Name { get; set; }

            [Required]
            public string Ticker { get; set; }
        }

        public class Handler : IRequestHandler<Command, StockListState>
        {
            private IAccountStorage _accountsStorage;
            private IPortfolioStorage _portfolioStorage;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage)
            {
                _accountsStorage = accounts;
                _portfolioStorage = storage;
            }

            public async Task<StockListState> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accountsStorage.GetUser(cmd.UserId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }
                
                var list = await _portfolioStorage.GetStockList(cmd.Name, user.State.Id);
                if (list == null)
                {
                    throw new InvalidOperationException("List does not exist");
                }

                list.AddStock(cmd.Ticker, null);

                await _portfolioStorage.Save(list, user.Id);

                return list.State;
            }
        }
    }
}