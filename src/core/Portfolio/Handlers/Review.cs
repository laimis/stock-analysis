using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Alerts;
using core.Options;
using core.Portfolio.Output;
using core.Shared;
using core.Stocks;

namespace core.Portfolio
{
    public class Review
    {
        public class Generate : RequestWithUserId<ReviewView>
        {
            public Generate(string period)
            {
                var dt = GetDateForPeriod(period);

                Start = dt.start;
                End = dt.end;
            }

            public DateTimeOffset Start { get; }
            public DateTimeOffset End { get; }

            public static (DateTimeOffset start, DateTimeOffset end) GetDateForPeriod(string period)
            {
                var start = DateTimeOffset.UtcNow.Date.AddDays(-7);
                var end = DateTimeOffset.UtcNow.Date.AddDays(1);

                if (period != "last7days")
                {
                    var date = DateTimeOffset.UtcNow.Date;
                    var toSubtract = (int)date.DayOfWeek - 1;
                    if (toSubtract < 0)
                    {
                        toSubtract = 6;
                    }

                    start = date.AddDays(-1 * toSubtract);
                    end = start.AddDays(7);
                }

                return (start, end);
            }
        }

        public class Handler : HandlerWithStorage<Generate, ReviewView>
        {
            public Handler(
                IPortfolioStorage storage,
                IAlertsStorage alerts,
                IStocksService2 stocks) : base(storage)
            {
                _alerts = alerts;
                _stocks = stocks;
            }

            private IAlertsStorage _alerts;
            private IStocksService2 _stocks;

            public override async Task<ReviewView> Handle(Generate request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);
                var stocks = await _storage.GetStocks(request.UserId);

                var transactions = options.SelectMany(o => o.State.Transactions)
                    .Union(stocks.SelectMany(s => s.State.Transactions))
                    .Where(t => t.DateAsDate >= request.Start)
                    .ToList();

                var stockTransactions = transactions.Where(t => !t.IsOption && !t.IsPL)
                    .GroupBy(t => t.Ticker)
                    .SelectMany(g => g)
                    .ToList();

                var optionTransactions = transactions.Where(t => t.IsOption && !t.IsPL)
                    .GroupBy(t => t.Ticker)
                    .SelectMany(g => g)
                    .ToList();

                var plStockTransactions = transactions.Where(t => !t.IsOption && t.IsPL)
                    .GroupBy(t => t.Ticker)
                    .SelectMany(g => g)
                    .ToList();

                var plOptionTransactions = transactions.Where(t => t.IsOption && t.IsPL)
                    .GroupBy(t => t.Ticker)
                    .SelectMany(g => g)
                    .ToList();

                return new ReviewView(
                    request.Start,
                    request.End,
                    stockTransactions,
                    optionTransactions,
                    plStockTransactions,
                    plOptionTransactions
                );
            }
        }
    }
}