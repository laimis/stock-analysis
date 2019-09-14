using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;
using Dapper;

namespace storage
{
    public class PortfolioStorage : AggregateStorage, IPortfolioStorage
    {
        const string _entity = "ownedstock";

        public PortfolioStorage(string cnn) : base(cnn)
        {
        }

        public async Task<OwnedStock> GetStock(string ticker, string userId)
        {
            var events = await GetEventsAsync(_entity, ticker, userId);
            
            return new OwnedStock(events);
        }

        public async Task Save(OwnedStock stock)
        {
            await SaveEventsAsync(stock, _entity);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            var list = await GetEventsAsync(_entity, userId);

            return list.GroupBy(e => e.Ticker)
                .Select(g => new OwnedStock(g.ToList()));
        }
    }
}