using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks;
using core.Stocks.Services.Trading;

namespace core.Portfolio
{
    public class SimulateTrades
    {
        public class Query : RequestWithUserId<List<TradingStrategyPerformance>>
        {
            public Query(bool closePositionIfOpenAtTheEnd, int numberOfTrades, Guid userId)
            {
                ClosePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd;
                NumberOfPositions = numberOfTrades;
                UserId = userId;
            }

            public bool ClosePositionIfOpenAtTheEnd { get; }
            public int NumberOfPositions { get; }
        }

        public class Handler
            : HandlerWithStorage<Query, List<TradingStrategyPerformance>>
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

            public override async Task<List<TradingStrategyPerformance>> Handle(Query request, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(request.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var stocks = await _storage.GetStocks(userId: request.UserId);
    
                var positions = stocks
                    .SelectMany(s => s.State.Positions)
                    .Where(s => s.IsClosed && s.RiskedAmount != null)
                    .OrderByDescending(p => p.Closed)
                    .Take(request.NumberOfPositions);
                
                var runner = new TradingStrategyRunner(_brokerage, _marketHours);

                var results = new List<TradingStrategyResult>();
                
                foreach(var position in positions)
                {
                    var simulatedResults = await runner.RunAsync(
                        user.State,
                        numberOfShares: position.CompletedPositionShares,
                        price: position.CompletedPositionCostPerShare,
                        stopPrice: position.FirstStop.Value,
                        ticker: position.Ticker,
                        when: position.Opened.Value,
                        closeIfOpenAtTheEnd: request.ClosePositionIfOpenAtTheEnd
                    );

                    results.Add(new TradingStrategyResult(0, 0, position, "Actual trading"));
                    results.AddRange(simulatedResults.Results);
                }


                var final = new List<TradingStrategyPerformance>();
                foreach(var strategyGroup in results.GroupBy(r => r.strategyName))
                {
                    var strategyPositions = strategyGroup.Select(r => r.position).ToArray();

                    var performance = TradingPerformanceView.Create(
                        new Span<PositionInstance>(strategyPositions)
                    );

                    final.Add(new TradingStrategyPerformance(strategyGroup.Key, performance, strategyPositions));
                }
                return final;
            }
        }
    }
}