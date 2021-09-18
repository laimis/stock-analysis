using System;
using System.Collections.Generic;
using System.Linq;
using core.Stocks;

namespace core.Reports.Views
{
    public class SellsView
    {
        public SellsView(IEnumerable<OwnedStock> stocks)
        {
            Sells = stocks
                    .SelectMany(s => s.State.BuyOrSell.Select(t => new { stock = s, buyOrSell = t}))
                    .Where(s => s.buyOrSell is StockSold)
                    .Where(s => s.buyOrSell.When > DateTimeOffset.UtcNow.AddDays(-60))
                    .GroupBy(s => s.stock.State.Ticker)
                    .Select(g => new {ticker = g.Key, latest = g.OrderByDescending(s => s.buyOrSell.When).First()})
                    .Select(t => new SellView
                    {
                        Ticker = t.ticker,
                        Date = t.latest.buyOrSell.When,
                        NumberOfShares = t.latest.buyOrSell.NumberOfShares,
                        Price = t.latest.buyOrSell.Price,
                        OlderThan30Days = t.latest.buyOrSell.When < DateTimeOffset.UtcNow.AddDays(-30)
                    })
                    .OrderByDescending(a => a.Date)
                    .ToList();
        }

        public List<SellView> Sells { get; }
    }
}