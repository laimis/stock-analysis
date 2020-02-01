using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using MediatR;

namespace core.Stocks
{
    public class Buy
    {
        public class Command : StockTransaction {}

        public class Handler : HandlerWithStorage<Command, Unit>
        {
            private IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Unit> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return new Unit();
                }

                // TODO: return error
                if (!user.IsConfirmed)
                {
                    return new Unit();
                }

                var stock = await this._storage.GetStock(cmd.Ticker, cmd.UserId);

                if (stock == null)
                {
                    stock = new OwnedStock(cmd.Ticker, cmd.UserId);
                }

                stock.Purchase(cmd.NumberOfShares, cmd.Price, cmd.Date.Value);

                await this._storage.Save(stock, cmd.UserId);

                return new Unit();
            }
        }
    }
}