using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
         Task RecordLoginAsync(string username);
    }
}