using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Portfolio;

namespace storage
{
    public class PortfolioStorage : AggregateStorage, IPortfolioStorage
    {
        const string _entityStock = "ownedstock";
        const string _entityOption = "ownedoption";

        public PortfolioStorage(string cnn) : base(cnn)
        {
        }

        public async Task<OwnedStock> GetStock(string ticker, string userId)
        {
            var events = await GetEventsAsync(_entityStock, ticker, userId);
            
            return new OwnedStock(events);
        }

        public async Task Save(OwnedStock stock)
        {
            await SaveEventsAsync(stock, _entityStock);
        }

        public async Task Save(OwnedOption option)
        {
            await SaveEventsAsync(option, _entityOption);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            var list = await GetEventsAsync(_entityStock, userId);

            return list.GroupBy(e => e.Ticker)
                .Select(g => new OwnedStock(g.ToList()));
        }

        public async Task<OwnedOption> GetOption(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice, string userId)
        {
            var key = OwnedOption.GenerateKey(ticker, optionType, expiration, strikePrice);

            var events = await GetEventsAsync(_entityOption, key, userId);

            return new OwnedOption(events);
        }
    }
}