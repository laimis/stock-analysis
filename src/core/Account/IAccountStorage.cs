using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account.Responses;

namespace core.Account
{
    public interface IAccountStorage
    {
        Task<AccountStatusView> ViewModel(Guid userId);
        Task SaveViewModel(AccountStatusView user, Guid userId);

        Task<User> GetUserByEmail(string emailAddress);
        Task<User> GetUser(Guid userId);
        Task Save(User u);
        Task Delete(User u);
        Task SaveUserAssociation(ProcessIdToUserAssociation r);
        Task<ProcessIdToUserAssociation> GetUserAssociation(Guid guid);
        Task<IEnumerable<(string email, string id)>> GetUserEmailIdPairs();
    }
}