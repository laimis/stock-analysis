using System;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using core.Stocks.Services.Trading;

namespace core.Portfolio
{
    public class SimulateTrade
    {
        public class Command : RequestWithUserId<PositionInstance>
        {
            public Command(int positionId, string strategyName, string ticker, Guid userId)
            {
                PositionId = positionId;
                StrategyName = strategyName;
                Ticker = ticker;
                UserId = userId;
            }

            public int PositionId { get; }
            public string StrategyName { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Command, PositionInstance>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            
            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<PositionInstance> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stock = await _storage.GetStock(ticker: request.Ticker, userId: request.UserId);
                if (stock == null)
                {
                    throw new Exception("Stock not found");
                }

                var position = stock.State.ClosedPositions[request.PositionId];
                if (position.FirstStop == null)
                {
                    throw new Exception("Position has no stop");
                }
                
                var runner = new TradingStrategyRunner(_brokerage);
                var strategy = TradingStrategyFactory.Create(request.StrategyName);

                return await runner.RunAsync(
                    user.State,
                    numberOfShares: position.FirstBuyNumberOfShares.Value,
                    price: position.FirstBuyCost.Value,
                    position.FirstStop.Value,
                    request.Ticker,
                    position.Opened.Value,
                    strategy
                );
            }
        }
    }
}