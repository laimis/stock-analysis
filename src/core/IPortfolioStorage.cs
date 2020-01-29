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
        Task<OwnedStock> GetStock(string ticker, string userId);
        Task<IEnumerable<OwnedStock>> GetStocks(string userId);
        Task Save(OwnedStock stock, string userId);

        Task<IEnumerable<OwnedOption>> GetSoldOptions(string user);
        Task<OwnedOption> GetSoldOption(Guid optionId, string userId);
        Task Save(OwnedOption option, string userId);
        
        Task Save(Note note, string userId);
        Task<IEnumerable<Note>> GetNotes(string userId);
        Task<Note> GetNote(string userId, Guid noteId);
    }
}