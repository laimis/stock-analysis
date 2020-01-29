using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Notes;
using core.Options;
using core.Stocks;

namespace coretests.Fakes
{
    public class FakePortfolioStorage : IPortfolioStorage
    {
        private OwnedOption _soldOption;
        private List<OwnedOption> _savedSoldOptions = new List<OwnedOption>();
        private List<Note> _notes = new List<Note>();

        public IEnumerable<OwnedOption> SavedOptions => _savedSoldOptions.AsReadOnly();

        public Task<OwnedOption> GetSoldOption(Guid id, string userId)
        {
            return Task.FromResult<OwnedOption>(_soldOption);
        }

        public Task<IEnumerable<OwnedOption>> GetSoldOptions(string user)
        {
            return Task.FromResult(
                new List<OwnedOption>{_soldOption}.Select(o => o)
            );
        }

        public Task<OwnedStock> GetStock(string ticker, string userId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OwnedStock>> GetStocks(string userId)
        {
            throw new NotImplementedException();
        }

        internal void Register(OwnedOption opt)
        {
            _soldOption = opt;
        }

        public Task Save(OwnedStock stock, string userId)
        {
            throw new NotImplementedException();
        }

        public Task Save(OwnedOption option, string userId)
        {
            _savedSoldOptions.Add(option);

            return Task.CompletedTask;
        }

        public Task Save(Note note, string userId)
        {
            _notes.Add(note);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Note>> GetNotes(string userId)
        {
            return Task.FromResult(_notes.Select(n => n));
        }

        public async Task<Note> GetNote(string userId, Guid noteId)
        {
            var list = await GetNotes(userId);

            return list.SingleOrDefault(n => n.State.Id == noteId);
        }
    }
}