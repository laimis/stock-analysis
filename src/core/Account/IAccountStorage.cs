using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
        Task<User> GetUserByEmail(string emailAddress);
        Task<User> GetUser(string userId);
        Task Save(User u);
        Task Delete(string userId, string email);
    }
}