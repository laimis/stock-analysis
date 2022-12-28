using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using MediatR;

namespace core.Portfolio.Handlers
{
    public class ListsCreate
    {
        public class Command : RequestWithUserId<StockListState>
        {
            [Required]
            public string Name { get; set; }

            public string Description { get; set; }
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
                if (list != null)
                {
                    throw new InvalidOperationException("List already exists");
                }

                list = new StockList(description: cmd.Description, name: cmd.Name, userId: user.State.Id);

                await _portfolioStorage.Save(list, user.Id);

                return list.State;
            }
        }
    }
}