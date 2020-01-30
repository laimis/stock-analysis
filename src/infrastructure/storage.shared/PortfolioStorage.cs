using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Notes;
using core.Options;
using core.Shared;
using core.Stocks;

namespace storage.shared
{
    public class PortfolioStorage : IPortfolioStorage
    {
        const string _stock_entity = "ownedstock3";
        const string _option_entity = "soldoption3";
        const string _note_entity = "note3";

        private IAggregateStorage _aggregateStorage;

        public PortfolioStorage(IAggregateStorage aggregateStorage)
        {
            _aggregateStorage = aggregateStorage;
        }

        public async Task<OwnedStock> GetStock(string ticker, string userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.State.Ticker == ticker);
        }

        public Task Save(OwnedStock stock, string userId)
        {
            return Save(stock, _stock_entity, userId);
        }

        public Task Save(OwnedOption option, string userId)
        {
            return Save(option, _option_entity, userId);
        }

        private Task Save(Aggregate agg, string entityName, string userId)
        {
            return _aggregateStorage.SaveEventsAsync(agg, entityName, userId);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedStock(g));
        }

        public async Task<OwnedOption> GetOwnedOption(Guid optionId, string userId)
        {
            return (await GetOwnedOptions(userId)).SingleOrDefault(s => s.State.Id == optionId);
        }

        public async Task<IEnumerable<OwnedOption>> GetOwnedOptions(string userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_option_entity, userId);

            var events = list.ToList();

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedOption(g));
        }

        public Task Save(Note note, string userId)
        {
            return Save(note, _note_entity, userId);
        }

        public async Task<IEnumerable<Note>> GetNotes(string userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_note_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new Note(g));
        }

        public async Task<Note> GetNote(string userId, Guid noteId)
        {
            var list = await GetNotes(userId);

            return list.SingleOrDefault(n => n.State.Id == noteId);
        }
    }
}