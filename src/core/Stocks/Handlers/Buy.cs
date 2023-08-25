using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;

namespace core.Stocks
{
    public class Buy
    {
        public class Command : StockTransaction {}

        public class Handler : HandlerWithStorage<Command, CommandResponse>
        {
            private readonly IAccountStorage _accounts;

            public Handler(IPortfolioStorage storage, IAccountStorage accounts) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<CommandResponse> Handle(Command cmd, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(cmd.UserId);
                if (user == null)
                {
                    return CommandResponse.Failed(
                        "Unable to find user account for stock operation");
                }

                if (user.State.Verified == null)
                {
                    return CommandResponse.Failed(
                        "Please verify your email first before you can record buy transaction");
                }

                var stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);
                stock ??= new OwnedStock(cmd.Ticker, cmd.UserId);

                var isNewPosition = false;
                if (stock.State.OpenPosition == null)
                {
                    isNewPosition = true;
                }

                stock.Purchase(cmd.NumberOfShares, cmd.Price, cmd.Date.Value, cmd.Notes, cmd.StopPrice);

                if (!string.IsNullOrEmpty(cmd.Strategy) && isNewPosition)
                {
                    stock.SetPositionLabel(stock.State.OpenPosition.PositionId, "strategy", cmd.Strategy);
                }

                await _storage.Save(stock, cmd.UserId);

                // see if we had pending position
                if (isNewPosition)
                {
                    var pendingPositions = await _storage.GetPendingStockPositions(cmd.UserId);
                    var found = pendingPositions.Where(x => x.State.Ticker == new Ticker(cmd.Ticker) && x.State.IsClosed == false)
                        .SingleOrDefault();
                        
                    if (found != null)
                    {
                        // in case we have open position but we recorded standalone, make sure
                        // that the properties get transferred over:
                        var pendingState = found.State;

                        // reload the state
                        stock = await _storage.GetStock(cmd.Ticker, cmd.UserId);

                        var opened = stock.State.OpenPosition;

                        var saveStock = false;
                        if (stock.SetStop(pendingState.StopPrice.Value))
                        {
                            saveStock = true;
                        }

                        if (opened.Notes.Count == 0)
                        {
                            if (stock.AddNotes(pendingState.Notes))
                            {
                                saveStock = true;
                            }
                        }

                        if (pendingState.Strategy != null)
                        {
                            if (stock.SetPositionLabel(positionId: opened.PositionId, key: "strategy", value: pendingState.Strategy))
                            {
                                saveStock = true;
                            }
                        }

                        if (saveStock)
                        {
                            await _storage.Save(stock, cmd.UserId);
                        }

                        found.Close(purchased: true, price: cmd.Price);

                        await _storage.Save(found, cmd.UserId);
                    }
                }

                return CommandResponse.Success();
            }
        }
    }
}