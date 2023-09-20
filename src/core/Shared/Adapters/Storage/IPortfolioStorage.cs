using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Portfolio;
using core.Stocks;

namespace core.Shared.Adapters.Storage
{
    public interface IPortfolioStorage
    {
        Task<T> ViewModel<T>(Guid userId, string version);
        Task SaveViewModel<T>(Guid userId, T model, string version);

        Task<OwnedStock> GetStock(string ticker, Guid userId);
        Task<OwnedStock> GetStock(Guid id, Guid userId);
        Task<IEnumerable<OwnedStock>> GetStocks(Guid userId);
        Task Save(OwnedStock stock, Guid userId);

        
        Task<IEnumerable<StockList>> GetStockLists(Guid userId);
        Task<StockList> GetStockList(string name, Guid userId);
        Task Save(StockList list, Guid userId);
        Task DeleteStockList(StockList list, Guid userId);

        Task Save(PendingStockPosition position, Guid userId);
        Task<IEnumerable<PendingStockPosition>> GetPendingStockPositions(Guid userId);
        
        Task<IEnumerable<Routine>> GetRoutines(Guid userId);
        Task Save(Routine routine, Guid userId);
        Task<Routine> GetRoutine(string name, Guid userId);
        Task DeleteRoutine(Routine routine, Guid userId);

        Task<IEnumerable<OwnedOption>> GetOwnedOptions(Guid userId);
        Task<OwnedOption> GetOwnedOption(Guid optionId, Guid userId);
        Task Save(OwnedOption option, Guid userId);
        
        Task Save(Note note, Guid userId);
        Task<IEnumerable<Note>> GetNotes(Guid userId);
        Task<Note> GetNote(Guid userId, Guid noteId);

        Task<OwnedCrypto> GetCrypto(string token, Guid userId);
        Task<OwnedCrypto> GetCrypto(Guid id, Guid userId);
        Task<IEnumerable<OwnedCrypto>> GetCryptos(Guid userId);
        Task Save(OwnedCrypto crypto, Guid userId);

        Task Delete(Guid userId);
    }
}