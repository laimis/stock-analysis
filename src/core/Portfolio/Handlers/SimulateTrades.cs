using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.CSV;
using core.Shared.Adapters.Storage;
using core.Stocks;
using core.Stocks.Services.Trading;
using MediatR;

namespace core.Portfolio.Handlers
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

        public class ExportQuery : RequestWithUserId<ExportResponse>
        {
            public ExportQuery(bool closePositionIfOpenAtTheEnd, int numberOfTrades, Guid userId)
            {
                ClosePositionIfOpenAtTheEnd = closePositionIfOpenAtTheEnd;
                NumberOfPositions = numberOfTrades;
                UserId = userId;
            }

            public bool ClosePositionIfOpenAtTheEnd { get; }
            public int NumberOfPositions { get; }
        }

        public class Handler
            : HandlerWithStorage<Query, List<TradingStrategyPerformance>>,
            IRequestHandler<ExportQuery, ExportResponse>
        {
            private IAccountStorage _accounts;
            private IBrokerage _brokerage;
            private ICSVWriter _csvWriter;
            private IMarketHours _marketHours;

            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                ICSVWriter csvWriter,
                IMarketHours marketHours,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
                _csvWriter = csvWriter;
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
                    .SelectMany(s => s.State.GetClosedPositions())
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

                    results.Add(new TradingStrategyResult(0, 0, 0, 0, position, TradingStrategyConstants.ACTUAL_TRADES_NAME));
                    results.AddRange(simulatedResults.Results);
                    if (simulatedResults.Failed)
                    {
                        results.Add(new TradingStrategyResult(0, 0, 0, 0, position, simulatedResults.FailedReason));
                    }
                }


                return results
                    .GroupBy(r => r.strategyName)
                    .Select(MapToStrategyPerformance)
                    .ToList();
            }

            private static TradingStrategyPerformance MapToStrategyPerformance(IGrouping<string, TradingStrategyResult> strategyGroup)
            {
                TradingPerformance CreatePerformance(PositionInstance[] positions)
                {
                    try
                    {
                        return TradingPerformance.Create(new Span<PositionInstance>(positions));
                    }
                    catch (OverflowException)
                    {
                        // TODO: something is throwing Value was either too large or too small for a Decimal
                        // for certain simulations.
                        // ignoring it here because I need the results, but need to look at it at some point
                        return TradingPerformance.Create(Span<PositionInstance>.Empty);
                    }
                }

                var strategyPositions = strategyGroup.Select(r => r.position).ToArray();

                var performance = CreatePerformance(strategyPositions);

                return new TradingStrategyPerformance(strategyGroup.Key, performance, strategyPositions);
            }

            public async Task<ExportResponse> Handle(ExportQuery request, CancellationToken cancellationToken)
            {
                var query = new Query(request.ClosePositionIfOpenAtTheEnd, request.NumberOfPositions, request.UserId);

                var results = await Handle(query, cancellationToken);

                var content = CSVExport.Generate(_csvWriter, results);

                var filename = CSVExport.GenerateFilename($"simulated-trades-{request.NumberOfPositions}");
                
                return new ExportResponse(filename, content);
            }
        }
    }
}