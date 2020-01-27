using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Shared;

namespace core.Portfolio
{
    public class Review
    {
        public class Generate : RequestWithUserId<IEnumerable<ReviewEntryGroup>>
        {
            public Generate(DateTime utcNow)
            {
                this.Date = utcNow;
            }

            public DateTime Date { get; }
        }

        public class Handler : HandlerWithStorage<Generate, IEnumerable<ReviewEntryGroup>>
        {
            public Handler(
                IPortfolioStorage storage,
                IStocksService2 stocks) : base(storage)
            {
                _stocks = stocks;
            }

            public IStocksService2 _stocks { get; }

            public override async Task<IEnumerable<ReviewEntryGroup>> Handle(Generate request, CancellationToken cancellationToken)
            {
                var options = _storage.GetSoldOptions(request.UserId);
                var stocks = _storage.GetStocks(request.UserId);
                var notes = _storage.GetNotes(request.UserId);

                await Task.WhenAll(options, stocks, notes);

                Console.WriteLine("Retrieved data");
                
                // open options
                var openOptions = options.Result.Where(s => s.State.IsOpen);

                var entries = new List<ReviewEntry>();

                foreach(var o in openOptions)
                {
                    entries.Add(new ReviewEntry{
                        IsOption = true,
                        Ticker = o.State.Ticker,
                        Description = $"${o.State.StrikePrice} {o.State.Type}",
                        Expiration = o.State.Expiration
                    });
                }

                foreach(var s in stocks.Result.Where(s => s.State.Owned > 0))
                {
                    entries.Add(new ReviewEntry{
                        IsShare = true,
                        Ticker = s.State.Ticker,
                        Description = $"{s.State.Owned} shares owned, cost of ${s.State.Cost}",
                    });
                }

                foreach(var n in notes.Result.Where(n => !string.IsNullOrWhiteSpace(n.State.RelatedToTicker)))
                {
                    entries.Add(new ReviewEntry {
                        Ticker = n.State.RelatedToTicker,
                        IsNote = true,
                        Description = n.State.Note,
                    });
                }

                var grouped = entries.GroupBy(r => r.Ticker);
                var groups = new List<ReviewEntryGroup>();

                foreach(var group in grouped)
                {
                    Console.WriteLine("getting " + group.Key + " price ");
                    var price = await _stocks.GetPrice(group.Key);
                    Console.WriteLine("finished " + group.Key + " price ");
                    groups.Add(new ReviewEntryGroup(group, price));
                }

                Console.WriteLine("returning result");

                return groups;
            }
        }
    }
}