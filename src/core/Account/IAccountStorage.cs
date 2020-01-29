using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
        Task<User> GetUser(string emailAddress);
        Task Save(User u);
    }
}