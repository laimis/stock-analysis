using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using core.Stocks.Services.Trading;
using MediatR;

namespace core.Portfolio
{
    public class SimulateTrade
    {
        public class Command : RequestWithUserId<TradingStrategyResults>
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

        public class ForTicker : RequestWithUserId<TradingStrategyResults>
        {
            public ForTicker(
                DateTimeOffset date,
                decimal numberOfShares,
                decimal price,
                decimal stopPrice,
                string strategyName,
                string ticker,
                Guid userId)
            {
                Date = date;
                NumberOfShares = numberOfShares;
                Price = price;
                StopPrice = stopPrice;
                StrategyName = strategyName;
                Ticker = ticker;
                UserId = userId;
            }

            public DateTimeOffset Date { get; }
            public decimal NumberOfShares { get; }
            public decimal Price { get; }
            public decimal StopPrice { get; }
            public string StrategyName { get; }
            public string Ticker { get; }
        }

        public class Handler
            : HandlerWithStorage<Command, TradingStrategyResults>,
            IRequestHandler<ForTicker, TradingStrategyResults>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private IMarketHours _marketHours;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IMarketHours marketHours,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _marketHours = marketHours;
            }

            public async Task<TradingStrategyResults> Handle(ForTicker request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var runner = new TradingStrategyRunner(_brokerage, _marketHours);
                
                return await runner.RunAsync(
                    user.State,
                    numberOfShares: request.NumberOfShares,
                    price: request.Price,
                    stopPrice: request.StopPrice,
                    ticker: request.Ticker,
                    when: request.Date
                );
            }

            public override async Task<TradingStrategyResults> Handle(Command request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stock = await _storage.GetStock(ticker: request.Ticker, userId: request.UserId);
                if (stock == null)
                {
                    throw new Exception($"Stock {request.Ticker} not found");
                }

                var position = stock.State.Positions.Where(p => p.PositionId == request.PositionId).FirstOrDefault();
                if (position == null)
                {
                    throw new Exception($"Position {request.PositionId} not found");
                }
                
                if (position.FirstStop == null)
                {
                    throw new Exception("Position has no stop");
                }
                
                var runner = new TradingStrategyRunner(_brokerage, _marketHours);
                
                return await runner.RunAsync(
                    user.State,
                    numberOfShares: position.CompletedPositionShares,
                    price: position.CompletedPositionCostPerShare,
                    position.FirstStop.Value,
                    request.Ticker,
                    position.Opened.Value
                );
            }
        }
    }
}