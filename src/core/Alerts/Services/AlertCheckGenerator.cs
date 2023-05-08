using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;
using core.Portfolio;

namespace core.Alerts.Services
{
    public static class AlertCheckGenerator
    {
        public static async Task<List<AlertCheck>> GetStocksFromListsWithTags(
            IPortfolioStorage portfolio,
            string tag,
            UserState user)
        {
            var list = await portfolio.GetStockLists(user.Id);

            return list
                .Where(l => l.State.ContainsTag(tag))
                .SelectMany(l => l.State.Tickers.Select(t => (l, t)))
                .Select(listTickerPair => new AlertCheck(ticker: listTickerPair.t.Ticker, listName: listTickerPair.l.State.Name, user: user))
                .ToList();
        }

        public static async Task<List<AlertCheck>> GetStopLossChecks(
            IPortfolioStorage portfolio,
            UserState user)
        {
            var list = await portfolio.GetStockLists(user.Id);
            
            var stocks = await portfolio.GetStocks(user.Id);
            return stocks
                .Where(s => s.State.OpenPosition != null)
                .Select(s => s.State.OpenPosition)
                .Where(p => p.StopPrice != null)
                .Select(p => new AlertCheck(
                    ticker: p.Ticker,
                    listName: "Stop Loss",
                    user: user,
                    threshold: p.StopPrice.Value
                ))
                .ToList();
        }
    }
}