using System;
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

        public Task Delete(User user)
        {
            _users.Remove(user.State.Email);

            return Task.CompletedTask;
        }

        public Task<User> GetUser(Guid userId)
        {
            return Task.FromResult(_users.Values.SingleOrDefault(v => v.State.Id == userId));
        }

        public Task<ProcessIdToUserAssociation> GetUserAssociation(Guid guid)
        {
            throw new NotImplementedException();
        }

        public Task<User> GetUserByEmail(string emailAddress)
        {
            return Task.FromResult(_users.GetValueOrDefault(emailAddress));
        }

        public Task<IEnumerable<(string, string)>> GetUsers()
        {
            throw new NotImplementedException();
        }

        public Task Save(User u)
        {
            _users[u.State.Email] = u;

            return Task.CompletedTask;
        }

        public Task SaveUserAssociation(ProcessIdToUserAssociation r)
        {
            throw new NotImplementedException();
        }
    }
}