using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Stocks.Services.Trading;

namespace core.Portfolio
{
    public class PricePoints
    {
        public class Query : RequestWithUserId<Stocks.Services.Trading.ProfitLevels.StrategyPricePoint[]>
        {
            public Query(int positionId, string ticker, Guid userId)
            {
                PositionId = positionId;
                Ticker = ticker;
                UserId = userId;
            }

            public int PositionId { get; }
            public string Ticker { get; }
        }

        public class Handler : HandlerWithStorage<Query, Stocks.Services.Trading.ProfitLevels.StrategyPricePoint[]>
        {
            private IAccountStorage _accounts;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Stocks.Services.Trading.ProfitLevels.StrategyPricePoint[]> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stock = await _storage.GetStock(request.Ticker, request.UserId);
                if (stock == null)
                {
                    throw new Exception("Stock not found");
                }

                var position = stock.State.Positions.Where(p => p.PositionId == request.PositionId).FirstOrDefault();
                if (position == null)
                {
                    throw new Exception("Position not found");
                }

                var stopBasedPricePoints =
                    Enumerable.Range(1, 4)
                    .Select(n => ProfitLevels.GetPricePointForProfitLevel(position, n).Value)
                    .ToArray();

                var percentBasePricePoints =
                    Enumerable.Range(1, 4)
                    .Select(n => ProfitLevels.GetPricePointForPercentLevels(position, n, percentGain: TradingStrategyRRLevels.AVG_PERCENT_GAIN).Value)
                    .ToArray();

                return new [] {
                    new ProfitLevels.StrategyPricePoint("Stop based", prices: stopBasedPricePoints),
                    new ProfitLevels.StrategyPricePoint($"{TradingStrategyRRLevels.AVG_PERCENT_GAIN}% intervals", prices: percentBasePricePoints)
                };
            }
        }
    }
}