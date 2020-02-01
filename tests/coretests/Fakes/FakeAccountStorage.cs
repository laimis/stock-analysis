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

        public Task Delete(string userId, string email)
        {
            return Task.CompletedTask;
        }

        public Task<User> GetUser(string userId)
        {
            return Task.FromResult(_users.Values.SingleOrDefault(v => v.State.Id.ToString() == userId));
        }

        public Task<User> GetUserByEmail(string emailAddress)
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