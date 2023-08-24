﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Stocks.View;
using MediatR;

namespace core.Stocks
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

                var view = new StockDashboardView(positions);

                var tickers = view.Positions.Select(o => o.Ticker).Distinct();

                var prices = await _brokerage.GetQuotes(user.State, tickers);
                if (prices.IsOk)
                {
                    EnrichWithStockPrice(view, prices.Success!);
                }

                if (user.State.ConnectedToBrokerage)
                {
                    var brokeragePositions = await _brokerage.GetAccount(user.State);
                    if (brokeragePositions.IsOk)
                    {
                        view.SetViolations(GetViolations(brokeragePositions.Success.StockPositions, view.Positions));
                    }
                }

                return view;
            }

            public static List<StockViolationView> GetViolations(IEnumerable<StockPosition> brokeragePositions, IEnumerable<PositionInstance> localPositions)
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

            private static StockDashboardView EnrichWithStockPrice(StockDashboardView view, Dictionary<string, StockQuote> prices)
            {
                foreach(var o in view.Positions)
                {
                    prices.TryGetValue(o.Ticker, out var price);
                    o.SetPrice(price?.Price ?? 0);
                }

                return view;
            }
        }
    }
}