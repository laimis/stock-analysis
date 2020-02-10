using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
        Task<User> GetUserByEmail(string emailAddress);
        Task<User> GetUser(Guid userId);
        Task Save(User u);
        Task Delete(User u);
        Task SaveUserAssociation(ProcessIdToUserAssociation r);
        Task<ProcessIdToUserAssociation> GetUserAssociation(Guid guid);
        Task<IEnumerable<(string, string)>> GetUsers();
    }
}