using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Stocks;
using core.Stocks.Services.Trading;

namespace core.Portfolio
{
    public class ProfitPoints
    {
        public class Query : RequestWithUserId<Stocks.Services.Trading.ProfitPoints.ProfitPointContainer[]>
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

        public class Handler : HandlerWithStorage<Query, Stocks.Services.Trading.ProfitPoints.ProfitPointContainer[]>
        {
            private IAccountStorage _accounts;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Stocks.Services.Trading.ProfitPoints.ProfitPointContainer[]> Handle(Query request, CancellationToken cancellationToken)
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

                var stopBasedProfitPoints = Stocks.Services.Trading.ProfitPoints.GetProfitPoints(Stocks.Services.Trading.ProfitPoints.GetProfitPointWithStopPrice, position, 4);

                Func<PositionInstance, int, decimal?> getPercentGain = (p, i) =>
                    Stocks.Services.Trading.ProfitPoints.GetProfitPointWithPercentGain(p, i, TradingStrategyConstants.AVG_PERCENT_GAIN);
                
                var percentBaseProfitPoints = Stocks.Services.Trading.ProfitPoints.GetProfitPoints(getPercentGain, position, 4);

                return new [] {
                    new Stocks.Services.Trading.ProfitPoints.ProfitPointContainer("Stop based", prices: stopBasedProfitPoints),
                    new Stocks.Services.Trading.ProfitPoints.ProfitPointContainer($"{TradingStrategyConstants.AVG_PERCENT_GAIN}% intervals", prices: percentBaseProfitPoints)
                };
            }
        }
    }
}