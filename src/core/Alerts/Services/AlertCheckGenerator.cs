using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;

namespace core.Alerts.Services
{
    public class AlertCheckGenerator
    {
        public static async Task<List<AlertCheck>> GetStocksFromListsWithTagsAsync(
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
    }
}