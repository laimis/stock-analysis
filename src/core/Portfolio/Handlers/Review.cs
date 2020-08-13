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
        public class Generate : RequestWithUserId<ReviewList>
        {
            public Generate(DateTimeOffset date)
            {
                this.Date = date;
            }

            public DateTimeOffset Date { get; }
        }

        public class Handler : HandlerWithStorage<Generate, ReviewList>
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

            public override async Task<ReviewList> Handle(Generate request, CancellationToken cancellationToken)
            {
                var options = await _storage.GetOwnedOptions(request.UserId);
                var stocks = await _storage.GetStocks(request.UserId);

                var start = request.Date.Date.AddDays(-7);
                var end = request.Date.Date;

                var groups = await CreateReviewGroups(options, stocks);

                var transactions = options.SelectMany(o => o.State.Transactions)
                    .Union(stocks.SelectMany(s => s.State.Transactions))
                    .Where(t => t.DateAsDate >= start)
                    .Where(t => !t.IsPL);

                return new ReviewList(
                    start,
                    end,
                    groups,
                    new TransactionList(transactions.Where(t => !t.IsOption), "ticker", null),
                    new TransactionList(transactions.Where(t => t.IsOption), "ticker", null)
                );
            }

            private async Task<List<ReviewEntryGroup>> CreateReviewGroups(
                IEnumerable<OwnedOption> options,
                IEnumerable<OwnedStock> stocks)
            {
                var entries = new List<ReviewEntry>();

                foreach (var o in options.Where(s => s.State.Active))
                {
                    entries.Add(new ReviewEntry(o));
                }

                foreach (var s in stocks.Where(s => s.State.Owned > 0))
                {
                    entries.Add(new ReviewEntry(s));
                }

                var grouped = entries.GroupBy(r => r.Ticker);
                var groups = new List<ReviewEntryGroup>();

                foreach (var group in grouped)
                {
                    var price = await _stocks.GetPrice(group.Key);
                    var advanced = await _stocks.GetAdvancedStats(group.Key);

                    groups.Add(new ReviewEntryGroup(group, price, advanced));
                }

                return groups;
            }
        }
    }
}