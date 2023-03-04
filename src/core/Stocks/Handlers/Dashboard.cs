using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Stocks;
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

        public class Handler : HandlerWithStorage<Query, object>,
            INotificationHandler<UserChanged>
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

            public override async Task<object> Handle(Query query, CancellationToken cancellationToken)
            {
                var user = await _accounts.GetUser(query.UserId);
                if (user == null)
                {
                    throw new Exception("User not found");
                }

                var view = await _storage.ViewModel<StockDashboardView>(query.UserId);
                if (view == null)
                {
                    view = await LoadFromDb(query.UserId);
                }

                var tickers = view.Positions.Select(o => o.Ticker).Distinct();

                var prices = await _brokerage.GetQuotes(user.State, tickers);
                if (prices.IsOk)
                {
                    EnrichWithStockPrice(view, prices.Success!);
                }

                if (user.State.ConnectedToBrokerage)
                {
                    var brokeragePositions = await _brokerage.GetPositions(user.State);
                    if (brokeragePositions.IsOk)
                    {
                        view.SetViolations(GetViolations(brokeragePositions.Success, view.Positions));
                    }
                }

                return view;
            }

            public static List<StockViolationView> GetViolations(IEnumerable<Position> brokeragePositions, IEnumerable<PositionInstance> localPositions)
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
                                    message: $"{brokeragePosition.Ticker} owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says {localPosition.NumberOfShares} @ ${localPosition.AverageCostPerShare}",
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
                                message: $"{brokeragePosition.Ticker} owned {brokeragePosition.Quantity} @ ${brokeragePosition.AverageCost} but NGTrading says none",
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
                                message: $"{localPosition.Ticker} owned {localPosition.NumberOfShares} but TDAmeritrade says none",
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
                                    message: $"{localPosition.Ticker} owned {localPosition.NumberOfShares} but TDAmeritrade says {brokeragePosition.Quantity}",
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

            private StockDashboardView EnrichWithStockPrice(StockDashboardView view, Dictionary<string, StockQuote> prices)
            {
                foreach(var o in view.Positions)
                {
                    prices.TryGetValue(o.Ticker, out var price);
                    o.SetPrice(price?.Price ?? 0);
                }

                return view;
            }

            public async Task Handle(UserChanged notification, CancellationToken cancellationToken)
            {
                var fromDb = await LoadFromDb(notification.UserId);

                await _storage.SaveViewModel(notification.UserId, fromDb);
            }

            private async Task<StockDashboardView> LoadFromDb(Guid userId)
            {
                var stocks = await _storage.GetStocks(userId);

                var positions = stocks.Where(s => s.State.OpenPosition != null)
                    .Select(s => s.State.OpenPosition)
                    .OrderBy(p => p.Ticker)
                    .ToList();

                return new StockDashboardView(positions);
            }
        }
    }
}