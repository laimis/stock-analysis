using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Portfolio.Views;
using core.Shared;

namespace core.Portfolio.Handlers
{
    public class TransactionSummary
    {
        public class Generate : RequestWithUserId<TransactionSummaryView>
        {
            public Generate(string period, Guid userId) : base(userId)
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

        public class Handler : HandlerWithStorage<Generate, TransactionSummaryView>
        {
            public Handler(IPortfolioStorage storage) : base(storage)
            {
            }

            public override async Task<TransactionSummaryView> Handle(Generate request, CancellationToken cancellationToken)
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
                    .OrderBy(t => t.DateAsDate)
                    .ToList();

                var plOptionTransactions = transactions.Where(t => t.IsOption && t.IsPL)
                    .GroupBy(t => t.Ticker)
                    .SelectMany(g => g)
                    .ToList();

                var closedPositions = stocks.SelectMany(s => s.State.Positions)
                    .Where(p => p.Closed >= request.Start && p.Closed <= request.End)
                    .ToList();

                var openPositions = stocks.Select(s => s.State.OpenPosition)
                    .Where(p => p != null)
                    .Where(p => p.Opened >= request.Start && p.Opened <= request.End)
                    .OrderBy(p => p.Opened)
                    .ToList();

                return new TransactionSummaryView(
                    start: request.Start,
                    end: request.End,
                    openPositions: openPositions,
                    closedPositions: closedPositions,
                    stockTransactions: stockTransactions,
                    optionTransactions: optionTransactions,
                    plStockTransactions: plStockTransactions,
                    plOptionTransactions: plOptionTransactions
                );
            }
        }
    }
}