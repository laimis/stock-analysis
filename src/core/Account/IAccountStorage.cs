using System;
using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
        Task<User> GetUserByEmail(string emailAddress);
        Task<User> GetUser(string userId);
        Task Save(User u);
        Task Delete(User u);
        Task SavePasswordResetRequest(PasswordResetRequest r);
        Task<PasswordResetRequest> GetPasswordResetRequest(Guid guid);
    }
}