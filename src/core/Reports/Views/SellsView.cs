using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Adapters.Stocks;
using core.Stocks;

namespace core.Reports.Views
{
    public class SellsView
    {
        public SellsView(List<SellView> sells) => Sells = sells;

        public List<SellView> Sells { get; }

        public static async Task<SellsView> Create(IEnumerable<OwnedStock> stocks, IStocksService2 priceFeed)
        {
            var filteredData = stocks
                    .SelectMany(s => s.State.BuyOrSell.Select(t => new { stock = s, buyOrSell = t}))
                    .Where(s => s.buyOrSell is StockSold)
                    .Where(s => s.buyOrSell.When > DateTimeOffset.UtcNow.AddDays(-60))
                    .GroupBy(s => s.stock.State.Ticker)
                    .Select(g => new {ticker = g.Key, latest = g.OrderByDescending(s => s.buyOrSell.When).First()});

            var prices = (await priceFeed.GetPrices(filteredData.Select(s => s.ticker))).Success;

            var sells = filteredData.Select(t => new SellView
                {
                    Ticker = t.ticker,
                    Date = t.latest.buyOrSell.When,
                    NumberOfShares = t.latest.buyOrSell.NumberOfShares,
                    Price = t.latest.buyOrSell.Price,
                    CurrentPrice = prices.ContainsKey(t.ticker) ? prices[t.ticker].Price : null,
                    OlderThan30Days = t.latest.buyOrSell.When < DateTimeOffset.UtcNow.AddDays(-30)
                })
                .OrderByDescending(a => a.Date)
                .ToList();
            
            return new SellsView(sells);
        }
    }
}