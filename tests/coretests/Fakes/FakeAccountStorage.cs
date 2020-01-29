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
        private Dictionary<string, User> _users = new Dictionary<string, User>();

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

        public Task<User> GetUser(string emailAddress)
        {
            return Task.FromResult(_users.GetValueOrDefault(emailAddress));
        }

        public Task Save(User u)
        {
            _users[u.State.Email] = u;

            return Task.CompletedTask;
        }
    }
}