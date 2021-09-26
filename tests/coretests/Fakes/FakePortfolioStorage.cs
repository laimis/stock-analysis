using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Cryptos;
using core.Notes;
using core.Options;
using core.Stocks;

namespace coretests.Fakes
{
    public class FakePortfolioStorage : IPortfolioStorage
    {
        private Dictionary<string, OwnedOption> _options = new Dictionary<string, OwnedOption>();
        private List<Note> _notes = new List<Note>();

        public IEnumerable<OwnedOption> SavedOptions => _options.Values.ToList();

        public Task<OwnedOption> GetOwnedOption(Guid id, Guid userId)
        {
            return Task.FromResult<OwnedOption>(_options.GetValueOrDefault(id.ToString()));
        }

        public Task<IEnumerable<OwnedOption>> GetOwnedOptions(Guid userId)
        {
            return Task.FromResult<IEnumerable<OwnedOption>>(
                SavedOptions
            );
        }

        public Task<OwnedStock> GetStock(string ticker, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OwnedStock>> GetStocks(Guid userId)
        {
            throw new NotImplementedException();
        }

        internal void Register(OwnedOption opt)
        {
            _options.Add(opt.State.Id.ToString(), opt);
        }

        public Task Save(OwnedStock stock, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task Save(OwnedOption option, Guid userId)
        {
            _options[option.State.Id.ToString()] = option;

            return Task.CompletedTask;
        }

        public Task Save(Note note, Guid userId)
        {
            _notes.Add(note);

            return Task.CompletedTask;
        }

        public Task<IEnumerable<Note>> GetNotes(Guid userId)
        {
            return Task.FromResult(_notes.Select(n => n));
        }

        public async Task<Note> GetNote(Guid userId, Guid noteId)
        {
            var list = await GetNotes(userId);

            return list.SingleOrDefault(n => n.State.Id == noteId);
        }

        public Task Delete(Guid userId)
        {
            return Task.CompletedTask;
        }

        public Task<OwnedStock> GetStock(Guid id, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<T> ViewModel<T>(Guid userId)
        {
            return Task.FromResult(default(T));
        }

        public Task SaveViewModel<T>(Guid userId, T model)
        {
            throw new NotImplementedException();
        }

        public Task<OwnedCrypto> GetCrypto(string token, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<OwnedCrypto> GetCrypto(Guid id, Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<OwnedCrypto>> GetCryptos(Guid userId)
        {
            throw new NotImplementedException();
        }

        public Task Save(OwnedCrypto crypto, Guid userId)
        {
            throw new NotImplementedException();
        }
    }
}