using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Options;
using core.Stocks;

namespace storage.shared
{
    public class PortfolioStorage : IPortfolioStorage
    {
        const string _stock_entity = "ownedstock";
        const string _option_entity = "soldoption";
        private IAggregateStorage _aggregateStorage;

        public PortfolioStorage(IAggregateStorage aggregateStorage)
        {
            _aggregateStorage = aggregateStorage;
        }

        public async Task<OwnedStock> GetStock(string ticker, string userId)
        {
            var events = await _aggregateStorage.GetEventsAsync(_stock_entity, ticker, userId);

            if (events.Count() == 0)
            {
                return null;
            }
            
            return new OwnedStock(events);
        }

        public async Task Save(OwnedStock stock)
        {
            await _aggregateStorage.SaveEventsAsync(stock, _stock_entity);
        }

        public async Task Save(SoldOption option)
        {
            await _aggregateStorage.SaveEventsAsync(option, _option_entity);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_entity, userId);

            return list.GroupBy(e => e.Ticker)
                .Select(g => new OwnedStock(g));
        }

        public async Task<SoldOption> GetSoldOption(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice, string userId)
        {
            var key = SoldOption.GenerateKey(ticker, optionType, expiration, strikePrice);

            var events = await _aggregateStorage.GetEventsAsync(_option_entity, key, userId);

            if (events.Count() == 0)
            {
                return null;
            }

            return new SoldOption(events);
        }

        public async Task<IEnumerable<SoldOption>> GetSoldOptions(string userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_option_entity, userId);

            var events = list.ToList();

            return list.GroupBy(e => e.Ticker)
                .Select(g => new SoldOption(g));
        }
    }
}