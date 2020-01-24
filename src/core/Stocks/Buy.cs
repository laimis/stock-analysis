using System.Threading;
using System.Threading.Tasks;
using MediatR;

namespace core.Stocks
{
    public class Buy
    {
        public class Handler : HandlerWithStorage<BuyCommand, Unit>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<Unit> Handle(BuyCommand cmd, CancellationToken cancellationToken)
            {
                var stock = await this._storage.GetStock(cmd.Ticker, cmd.UserId);

                if (stock == null)
                {
                    stock = new OwnedStock(cmd.Ticker, cmd.UserId);
                }

                stock.Purchase(cmd.Amount, cmd.Price, cmd.Date.Value);

                await this._storage.Save(stock);

                return new Unit();
            }
        }
    }
}