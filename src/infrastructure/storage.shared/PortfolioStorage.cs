using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Cryptos;
using core.fs.Accounts;
using core.fs.Adapters.Storage;
using core.fs.Stocks;
using core.Options;
using core.Routines;
using core.Shared;
using core.Stocks;
using Microsoft.FSharp.Core;

namespace storage.shared
{
    public class PortfolioStorage : IPortfolioStorage
    {
        public const string _stock_entity = "ownedstock3";
        public const string _option_entity = "soldoption3";
        public const string _crypto_entity = "ownedcrypto";
        public const string _stock_list_entity = "stocklist";
        public const string _routine_entity = "routine";
        public const string _pending_stock_position_entity = "pendingstockposition";
        public const string _stock_position_entity = "stockposition";
        
        public const string _note_entity = "note3"; // used to be used for stocks but it was modeled "weirdly" and ditched with of the software

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

        internal async Task<OwnedStock> GetStock(Ticker ticker, UserId userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.State.Ticker.Equals(ticker));
        }

        internal async Task<OwnedStock> GetStockByStockId(Guid stockId, UserId userId)
        {
            var stocks = await GetStocks(userId);
            
            return stocks.SingleOrDefault(s => s.Id == stockId);
        }

        internal Task Save(OwnedStock stock, UserId userId)
        {
            return Save(stock, _stock_entity, userId);
        }

        public async Task<IEnumerable<StockPositionState>> GetStockPositions(UserId userId)
        {
            var events = await _aggregateStorage.GetEventsAsync(_stock_position_entity, userId);
            
            return events.GroupBy(e => e.AggregateId)
                .Select(StockPosition.createFromEvents);
        }

        public async Task<FSharpOption<StockPositionState>> GetStockPosition(StockPositionId positionId, UserId userId)
        {
            var events = await _aggregateStorage.GetEventsAsync(
                _stock_position_entity, positionId.Item, userId);

            var list = events.ToList();
            
            if (list.Count == 0)
            {
                return FSharpOption<StockPositionState>.None;
            }
            
            return FSharpOption<StockPositionState>.Some(StockPosition.createFromEvents(list));
        }

        public Task SaveStockPosition(UserId userId, FSharpOption<StockPositionState> previousState, StockPositionState newState)
        {
            return _aggregateStorage.SaveEventsAsync(FSharpOption<StockPositionState>.get_IsNone(previousState) ? null : previousState.Value , newState, _stock_position_entity, userId);
        }
        
        public async Task DeleteStockPosition(UserId userId, FSharpOption<StockPositionState> previousState, StockPositionState state)
        {
            // first save so that we persist deleted event
            await SaveStockPosition(userId, previousState, state);
            
            await _aggregateStorage.DeleteAggregate(_stock_position_entity, state.PositionId.Item, userId);
        }

        public Task SaveOwnedOption(OwnedOption option, UserId userId)
        {
            return Save(option, _option_entity, userId);
        }

        private Task Save(IAggregate agg, string entityName, UserId userId)
        {
            return _aggregateStorage.SaveEventsAsync(agg, entityName, userId);
        }

        internal async Task<IEnumerable<OwnedStock>> GetStocks(UserId userId)
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

        public async Task Delete(UserId userId)
        {
            await _aggregateStorage.DeleteAggregates(_note_entity, userId);
            await _aggregateStorage.DeleteAggregates(_option_entity, userId);
            await _aggregateStorage.DeleteAggregates(_stock_entity, userId);
            await _aggregateStorage.DeleteAggregates(_crypto_entity, userId);
            await _aggregateStorage.DeleteAggregates(_stock_list_entity, userId);
            await _aggregateStorage.DeleteAggregates(_pending_stock_position_entity, userId);
            await _aggregateStorage.DeleteAggregates(_stock_position_entity, userId);
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

        public async Task<Routine> GetRoutine(Guid id, UserId userId)
        {
            var list = await GetRoutines(userId);
            
            return list.SingleOrDefault(s => s.State.Id == id);
        }

        public Task DeleteRoutine(Routine routine, UserId userId) =>
            _aggregateStorage.DeleteAggregate(entity: _routine_entity, aggregateId: routine.Id, userId: userId);

        public async Task<IEnumerable<StockList>> GetStockLists(UserId userId)
        {
            var list = await _aggregateStorage.GetEventsAsync(_stock_list_entity, userId);

            return list.GroupBy(e => e.AggregateId)
                .Select(g => new StockList(g));
        }

        public async Task<StockList> GetStockList(Guid id, UserId userId)
        {
            var list = await GetStockLists(userId);
            
            return list.SingleOrDefault(s => s.State.Id == id);
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
