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
        public class Query : RequestWithUserId<Stocks.Services.Trading.ProfitLevels.ProfitPoints[]>
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

        public class Handler : HandlerWithStorage<Query, Stocks.Services.Trading.ProfitLevels.ProfitPoints[]>
        {
            private IAccountStorage _accounts;

            public Handler(IAccountStorage accounts, IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
            }

            public override async Task<Stocks.Services.Trading.ProfitLevels.ProfitPoints[]> Handle(Query request, CancellationToken cancellationToken)
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

                var stopBasedProfitPoints = ProfitLevels.GetProfitPoints(ProfitLevels.GetProfitPoint, position, 4);

                Func<PositionInstance, int, decimal?> getPercentGain = (p, i) => 
                    ProfitLevels.GetProfitPointForPercentGain(p, i, TradingStrategyRRLevels.AVG_PERCENT_GAIN);
                
                var percentBaseProfitPoints = ProfitLevels.GetProfitPoints(getPercentGain, position, 4);

                return new [] {
                    new ProfitLevels.ProfitPoints("Stop based", prices: stopBasedProfitPoints),
                    new ProfitLevels.ProfitPoints($"{TradingStrategyRRLevels.AVG_PERCENT_GAIN}% intervals", prices: percentBaseProfitPoints)
                };
            }
        }
    }
}