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
        Task Save(OwnedStock stock);

        Task<IEnumerable<SoldOption>> GetSoldOptions(string user);
        Task<SoldOption> GetSoldOption(string ticker, OptionType optionType, DateTimeOffset expiration, double strikePrice, string userId);
        Task Save(SoldOption option);
        
        Task Save(Note note);
        Task<IEnumerable<Note>> GetNotes(string userId);
        Task<Note> GetNote(string userId, string noteId);
    }
}