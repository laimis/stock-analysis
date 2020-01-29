using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Notes;
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
            public Generate(DateTime date)
            {
                this.Date = date;
            }

            public DateTime Date { get; }
        }

        public class Handler : HandlerWithStorage<Generate, ReviewList>
        {
            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stocks) : base(storage)
            {
                _stocks = stocks;
            }

            private IStocksService2 _stocks { get; }

            public override async Task<ReviewList> Handle(Generate request, CancellationToken cancellationToken)
            {
                var options = _storage.GetSoldOptions(request.UserId);
                var stocks = _storage.GetStocks(request.UserId);
                var notes = _storage.GetNotes(request.UserId);

                await Task.WhenAll(options, stocks, notes);

                var groups = await CreateReviewGroups(options, stocks, notes);

                var filterDate = request.Date.Date.AddDays(-7);

                var transactions = options.Result.SelectMany(o => o.State.Transactions)
                    .Union(stocks.Result.SelectMany(s => s.State.Transactions))
                    .Where(t => t.Date > filterDate);

                return new ReviewList {
                    Tickers = groups,
                    TransactionList = new TransactionList(transactions, true)
                };
            }

            private async Task<List<ReviewEntryGroup>> CreateReviewGroups(Task<IEnumerable<OwnedOption>> options, Task<IEnumerable<OwnedStock>> stocks, Task<IEnumerable<Note>> notes)
            {
                var entries = new List<ReviewEntry>();

                foreach (var o in options.Result.Where(s => s.State.IsOpen))
                {
                    entries.Add(new ReviewEntry
                    {
                        Ticker = o.State.Ticker,
                        Description = $"${o.State.StrikePrice} {o.State.OptionType}",
                        Expiration = o.State.Expiration
                    });
                }

                foreach (var s in stocks.Result.Where(s => s.State.Owned > 0))
                {
                    entries.Add(new ReviewEntry
                    {
                        Ticker = s.State.Ticker,
                        Description = $"{s.State.Owned} shares owned, cost of ${s.State.Cost}",
                    });
                }

                foreach (var n in notes.Result.Where(n => !string.IsNullOrWhiteSpace(n.State.RelatedToTicker)))
                {
                    entries.Add(new ReviewEntry
                    {
                        Ticker = n.State.RelatedToTicker,
                        IsNote = true,
                        Description = n.State.Note,
                    });
                }

                var grouped = entries.GroupBy(r => r.Ticker);
                var groups = new List<ReviewEntryGroup>();

                foreach (var group in grouped)
                {
                    var price = await _stocks.GetPrice(group.Key);

                    groups.Add(new ReviewEntryGroup(group, price));
                }

                return groups;
            }
        }
    }
}