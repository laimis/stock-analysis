using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Shared;
using core.Stocks;

namespace storage.shared
{
    public class PortfolioStorage : IPortfolioStorage
    {
        private const string _stock_entity = "ownedstock3";
        private const string _option_entity = "soldoption3";
        private const string _note_entity = "note3";
        private const string _crypto_entity = "ownedcrypto";

        private IAggregateStorage _aggregateStorage;
        private IBlobStorage _blobStorage;

        public PortfolioStorage(
            IAggregateStorage aggregateStorage,
            IBlobStorage blobStorage)
        {
            _aggregateStorage = aggregateStorage;
            _blobStorage = blobStorage;
        }

        public Task<T> ViewModel<T>(Guid userId)
        {
            return _blobStorage.Get<T>(typeof(T).Name + "#" + userId);
        }

        public Task SaveViewModel<T>(Guid userId, T t)
        {
            return _blobStorage.Save<T>(typeof(T).Name + "#" + userId, t);
        }

        public async Task<OwnedStock> GetStock(string ticker, Guid userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.State.Ticker == ticker);
        }

        public async Task<OwnedStock> GetStock(Guid stockId, Guid userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.Id == stockId);
        }

        public Task Save(OwnedStock stock, Guid userId)
        {
            return Save(stock, _stock_entity, userId);
        }

        public Task Save(OwnedOption option, Guid userId)
        {
            return Save(option, _option_entity, userId);
        }

        private Task Save(Aggregate agg, string entityName, Guid userId)
        {
            return _aggregateStorage.SaveEventsAsync(agg, entityName, userId);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(Guid userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedStock(g));
        }

        public async Task<OwnedOption> GetOwnedOption(Guid optionId, Guid userId)
        {
            return (await GetOwnedOptions(userId)).SingleOrDefault(s => s.State.Id == optionId);
        }

        public async Task<IEnumerable<OwnedOption>> GetOwnedOptions(Guid userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_option_entity, userId);

            var events = list.ToList();

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedOption(g));
        }

        public Task Save(Note note, Guid userId)
        {
            return Save(note, _note_entity, userId);
        }

        public async Task<IEnumerable<Note>> GetNotes(Guid userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_note_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new Note(g));
        }

        public async Task<Note> GetNote(Guid userId, Guid noteId)
        {
            var list = await GetNotes(userId);

            return list.SingleOrDefault(n => n.State.Id == noteId);
        }

        public async Task Delete(Guid userId)
        {
            await this._aggregateStorage.DeleteAggregates(_note_entity, userId);
            await this._aggregateStorage.DeleteAggregates(_option_entity, userId);
            await this._aggregateStorage.DeleteAggregates(_stock_entity, userId);
            await this._aggregateStorage.DeleteAggregates(_crypto_entity, userId);
            await this._aggregateStorage.DeleteAggregates(AlertsStorage._alert_entity, userId);
        }

        public async Task<OwnedCrypto> GetCrypto(string token, Guid userId)
        {
            var cryptos = await GetCryptos(userId);
            
            return cryptos.SingleOrDefault(s => s.State.Token == token);
        }

        public async Task<OwnedCrypto> GetCrypto(Guid cryptoId, Guid userId)
        {
            var cryptos = await GetCryptos(userId);
            
            return cryptos.SingleOrDefault(s => s.Id == cryptoId);
        }

        public Task Save(OwnedCrypto crypto, Guid userId)
        {
            return Save(crypto, _crypto_entity, userId);
        }

        public async Task<IEnumerable<OwnedCrypto>> GetCryptos(Guid userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_crypto_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedCrypto(g));
        }
    }
}