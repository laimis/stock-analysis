using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Stocks
{
    public class Sell
    {
        public class Handler : HandlerWithStorage<SellTransaction, Unit>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<Unit> Handle(SellTransaction cmd, CancellationToken cancellationToken)
            {
                var stock = await this._storage.GetStock(cmd.Ticker, cmd.UserId);

                if (stock == null)
                {
                    return new Unit();
                }

                stock.Sell(cmd.Amount, cmd.Price, cmd.Date.Value);

                await this._storage.Save(stock);

                return new Unit();
            }
        }
    }
}