using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Storage;
using core.Stocks.View;

namespace core.Stocks.Handlers
{
    public class Dashboard
    {
        public class Query : RequestWithUserId<object>
        {
            public Query(Guid userId) : base(userId)
            {
            }
        }

        public class Handler : HandlerWithStorage<Query, object>
        {
            private readonly IAccountStorage _accounts;
            private readonly IBrokerage _brokerage;
            
            public Handler(
                IAccountStorage accounts,
                IBrokerage brokerage,
                IPortfolioStorage storage) : base(storage)
            {
                _accounts = accounts;
                _brokerage = brokerage;
            }

            public override async Task<object> Handle(Query query, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(query.UserId)
                    ?? throw new Exception("User not found");
                    
                var stocks = await _storage.GetStocks(query.UserId);

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .OrderBy(p => p.Ticker)
                    .ToList();

                async Task<StockPosition[]> GetBrokeragePositions()
                {
                    var brokeragePositions = await _brokerage.GetAccount(user.State);
                    return brokeragePositions.IsOk switch {
                        true => brokeragePositions.Success.StockPositions,
                        false => Array.Empty<StockPosition>()
                    };
                }

                var brokeragePositions = await (user.State.ConnectedToBrokerage switch {
                    false => Task.FromResult(Array.Empty<StockPosition>()),
                    true => GetBrokeragePositions()
                });

                async Task<List<StockViolationView>> GetViolationsAsync(Dictionary<string, StockQuote> priceDictionary)
                {
                    var brokeragePositions = await _brokerage.GetAccount(user.State);
                    if (brokeragePositions.IsOk)
                    {
                        return GetViolations(brokeragePositions.Success.StockPositions, positions, priceDictionary);
                    }
                    return new List<StockViolationView>();
                }

                var tickers = positions.Select(o => o.Ticker).Union(brokeragePositions.Select(v => v.Ticker)).Distinct();
                var quotesResult = await _brokerage.GetQuotes(user.State, tickers);
                var priceDictionary = quotesResult.Success ?? new Dictionary<string, StockQuote>();

                var violations = await (
                    user.State.ConnectedToBrokerage switch {
                        false => Task.FromResult(new List<StockViolationView>()),
                        true => GetViolationsAsync(priceDictionary)
                    });

                foreach(var p in positions)
                {
                    if (priceDictionary.TryGetValue(p.Ticker, out var quote))
                    {
                        p.SetPrice(quote.Price);
                    }
                }

                return new StockDashboardView(positions, violations);
            }

            public static List<StockViolationView> GetViolations(IEnumerable<StockPosition> brokeragePositions, IEnumerable<PositionInstance> localPositions, Dictionary<string, StockQuote> priceDictionary)
            {
                var violations = new HashSet<StockViolationView>();

                // go through each position and see if it's recorded in portfolio, and quantity matches
                foreach (var brokeragePosition in brokeragePositions)
                {
                    var localPosition = localPositions.SingleOrDefault(o => o.Ticker == brokeragePosition.Ticker);
                    if (localPosition != null)
                    {
                        if (localPosition.NumberOfShares != brokeragePosition.Quantity)
                        {
                            violations.Add(
                                new StockViolationView(
                                    currentPrice: priceDictionary.TryGetValue(brokeragePosition.Ticker, out var quote) ? quote.Price : null,
                                    message: $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare}",
                                    numberOfShares: brokeragePosition.Quantity,
                                    pricePerShare: brokeragePosition.AverageCost,
                                    ticker: brokeragePosition.Ticker
                                )
                            );
                        }
                    }
                    else
                    {
                        violations.Add(
                            new StockViolationView(
                                currentPrice: priceDictionary.TryGetValue(brokeragePosition.Ticker, out var quote) ? quote.Price : null,
                                message: $"Owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says none",
                                numberOfShares: brokeragePosition.Quantity,
                                pricePerShare: brokeragePosition.AverageCost,
                                ticker: brokeragePosition.Ticker
                            )
                        );
                    }
                }

                // go through each portfolion and see if it's in positions
                foreach (var localPosition in localPositions)
                {
                    var brokeragePosition = brokeragePositions.SingleOrDefault(p => p.Ticker == localPosition.Ticker);
                    if (brokeragePosition == null)
                    {
                        violations.Add(
                            new StockViolationView(
                                currentPrice: priceDictionary.TryGetValue(localPosition.Ticker, out var quote) ? quote.Price : null,
                                message: $"Owned {localPosition.NumberOfShares} but TDAmeritrade says none",
                                numberOfShares: localPosition.NumberOfShares,
                                pricePerShare: localPosition.AverageCostPerShare,
                                ticker: localPosition.Ticker
                            )
                        );
                    }
                    else
                    {
                        if (brokeragePosition.Quantity != localPosition.NumberOfShares)
                        {
                            violations.Add(
                                new StockViolationView(
                                    currentPrice: priceDictionary.TryGetValue(localPosition.Ticker, out var quote) ? quote.Price : null,
                                    message: $"Owned {localPosition.NumberOfShares} but TDAmeritrade says {brokeragePosition.Quantity}",
                                    numberOfShares: localPosition.NumberOfShares,
                                    pricePerShare: localPosition.AverageCostPerShare,
                                    ticker: localPosition.Ticker
                                )
                            );
                        }
                    }
                }

                return violations.OrderBy(v => v.Ticker).ToList();
            }
        }
    }
}