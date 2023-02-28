using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;

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
                .SelectMany(x => x.State.Tickers)
                .Select(t => new AlertCheck(ticker: t.Ticker, user: user))
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
                    user: user,
                    threshold: p.StopPrice.Value
                ))
                .ToList();
        }
    }
}