using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Cryptos;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Notes;
using core.Options;
using core.Portfolio;
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
        private const string _stock_list_entity = "stocklist";
        private const string _routine_entity = "routine";
        private const string _pending_stock_position_entity = "pendingstockposition";

        private readonly IAggregateStorage _aggregateStorage;
        private readonly IBlobStorage _blobStorage;

        public PortfolioStorage(
            IAggregateStorage aggregateStorage,
            IBlobStorage blobStorage)
        {
            _aggregateStorage = aggregateStorage;
            _blobStorage = blobStorage;
        }

        public Task<T> ViewModel<T>(Guid userId, string version)
        {
            return _blobStorage.Get<T>(typeof(T).Name + "#" + version + "#" + userId);
        }

        public Task SaveViewModel<T>(Guid userId, T t, string version)
        {
            return _blobStorage.Save(typeof(T).Name + "#" + version + "#" + userId, t);
        }

        public async Task<OwnedStock> GetStock(string ticker, UserId userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.State.Ticker == ticker);
        }

        public async Task<OwnedStock> GetStockByStockId(Guid stockId, UserId userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.Id == stockId);
        }

        public Task Save(OwnedStock stock, UserId userId)
        {
            return Save(stock, _stock_entity, userId);
        }

        public Task SaveOwnedOption(OwnedOption option, UserId userId)
        {
            return Save(option, _option_entity, userId);
        }

        private Task Save(IAggregate agg, string entityName, UserId userId)
        {
            return _aggregateStorage.SaveEventsAsync(agg, entityName, userId);
        }

        public async Task<IEnumerable<OwnedStock>> GetStocks(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedStock(g));
        }

        public async Task<OwnedOption> GetOwnedOption(Guid optionId, UserId userId)
        {
            return (await GetOwnedOptions(userId)).SingleOrDefault(s => s.State.Id == optionId);
        }

        public async Task<IEnumerable<OwnedOption>> GetOwnedOptions(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_option_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedOption(g))
                .Where(g => g.State.Deleted == false);
        }

        public Task SaveNote(Note note, UserId userId)
        {
            return Save(note, _note_entity, userId);
        }

        public async Task<IEnumerable<Note>> GetNotes(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_note_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new Note(g));
        }

        public async Task<Note> GetNote(Guid noteId, UserId userId)
        {
            var list = await GetNotes(userId);

            return list.SingleOrDefault(n => n.State.Id == noteId);
        }

        public async Task Delete(UserId userId)
        {
            await _aggregateStorage.DeleteAggregates(_note_entity, userId);
            await _aggregateStorage.DeleteAggregates(_option_entity, userId);
            await _aggregateStorage.DeleteAggregates(_stock_entity, userId);
            await _aggregateStorage.DeleteAggregates(_crypto_entity, userId);
            await _aggregateStorage.DeleteAggregates(_stock_list_entity, userId);
            await _aggregateStorage.DeleteAggregates(_pending_stock_position_entity, userId);
        }

        public async Task<OwnedCrypto> GetCrypto(string token, UserId userId)
        {
            var cryptos = await GetCryptos(userId);
            
            return cryptos.SingleOrDefault(s => s.State.Token == token);
        }

        public async Task<OwnedCrypto> GetCryptoByCryptoId(Guid cryptoId, UserId userId)
        {
            var cryptos = await GetCryptos(userId);
            
            return cryptos.SingleOrDefault(s => s.Id == cryptoId);
        }

        public Task SaveCrypto(OwnedCrypto crypto, UserId userId)
        {
            return Save(crypto, _crypto_entity, userId);
        }

        public async Task<IEnumerable<OwnedCrypto>> GetCryptos(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_crypto_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new OwnedCrypto(g));
        }

        public async Task<IEnumerable<Routine>> GetRoutines(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_routine_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new Routine(g));
        }

        public Task SaveRoutine(Routine routine, UserId userId) =>
            Save(routine, _routine_entity, userId);

        public async Task<Routine> GetRoutine(string name, UserId userId)
        {
            var list = await GetRoutines(userId);
            
            return list.SingleOrDefault(s => s.State.Name == name);
        }

        public Task DeleteRoutine(Routine routine, UserId userId) =>
            _aggregateStorage.DeleteAggregate(entity: _routine_entity, aggregateId: routine.Id, userId: userId);

        public async Task<IEnumerable<StockList>> GetStockLists(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_list_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new StockList(g));
        }

        public async Task<StockList> GetStockList(string name, UserId userId)
        {
            var list = await GetStockLists(userId);
            
            return list.SingleOrDefault(s => s.State.Name == name);
        }

        public Task SaveStockList(StockList list, UserId userId) =>
            Save(list, _stock_list_entity, userId);

        public Task DeleteStockList(StockList list, UserId userId) =>
            _aggregateStorage.DeleteAggregate(entity: _stock_list_entity, aggregateId: list.Id, userId: userId);

        public Task SavePendingPosition(PendingStockPosition position, UserId userId) =>
            Save(position, _pending_stock_position_entity, userId);

        public Task<IEnumerable<PendingStockPosition>> GetPendingStockPositions(UserId userId) =>
            _aggregateStorage.GetEventsAsync(_pending_stock_position_entity, userId)
                .ContinueWith(t => t.Result.GroupBy(e => e.AggregateId)
                    .Select(g => new PendingStockPosition(g)));
    }
}