using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;

namespace coretests.Fakes
{
    public class FakeAccountStorage : IAccountStorage
    {
        private List<LoginLogEntry> _saved = new List<LoginLogEntry>();
        private List<LoginLogEntry> _logins = new List<LoginLogEntry>();

        public IReadOnlyList<LoginLogEntry> SavedEntries => _saved.AsReadOnly();

        public void Register(LoginLogEntry entry)
        {
            _logins.Add(entry);
        }

        public Task<IEnumerable<LoginLogEntry>> GetLogins()
        {
            return Task.FromResult(_logins.Select(e => e));
        }

        public Task RecordLoginAsync(LoginLogEntry entry)
        {
            _saved.Add(entry);
            
            return Task.CompletedTask;
        }
    }
}