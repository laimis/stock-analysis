using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core.Account;

namespace coretests.Fakes
{
    public class FakeAccountStorage : IAccountStorage
    {
        private Dictionary<string, User> _users = new Dictionary<string, User>();

        public IReadOnlyList<User> SavedEntries => _users.Values.ToList();

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