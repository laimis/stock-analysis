using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Notes;
using core.Options;
using core.Stocks;

namespace core
{
    public interface IPortfolioStorage
    {
        Task<OwnedStock> GetStock(string ticker, Guid userId);
        Task<OwnedStock> GetStock(Guid id, Guid userId);
        Task<IEnumerable<OwnedStock>> GetStocks(Guid userId);
        Task Save(OwnedStock stock, Guid userId);

        Task<IEnumerable<OwnedOption>> GetOwnedOptions(Guid userId);
        Task<OwnedOption> GetOwnedOption(Guid optionId, Guid userId);
        Task Save(OwnedOption option, Guid userId);
        
        Task Save(Note note, Guid userId);
        Task<IEnumerable<Note>> GetNotes(Guid userId);
        Task<Note> GetNote(Guid userId, Guid noteId);
        Task Delete(Guid userId);
    }
}