using System;
using System.Collections.Generic;
using System.Linq;
using core.Shared.Adapters.Brokerage;
using core.Stocks;

namespace core.Reports.Views
{
    public class SellView
    {
        public string Ticker { get; set; }
        public DateTimeOffset Date { get; set; }
        public decimal NumberOfShares { get; set; }
        public decimal Price { get; set; }
        public bool OlderThan30Days { get; set; }
        public int NumberOfDays => (int)Math.Floor(DateTimeOffset.UtcNow.Subtract(Date).TotalDays);
        public decimal? CurrentPrice { get; set; }
        public decimal? Diff => CurrentPrice.HasValue ? (CurrentPrice.Value - Price) / Price : null;
    }
    
    public class SellsView
    {
        public SellsView(List<SellView> sells) => Sells = sells;

        public List<SellView> Sells { get; }

        public static SellsView Create(IEnumerable<OwnedStock> stocks, Dictionary<string, StockQuote> prices)
        {
            var filteredData = stocks
                    .SelectMany(s => s.State.BuyOrSell.Select(t => new { stock = s, buyOrSell = t}))
                    .Where(s => s.buyOrSell is StockSold)
                    .Where(s => s.buyOrSell.When > DateTimeOffset.UtcNow.AddDays(-60))
                    .GroupBy(s => s.stock.State.Ticker)
                    .Select(g => new {ticker = g.Key, latest = g.OrderByDescending(s => s.buyOrSell.When).First()});

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