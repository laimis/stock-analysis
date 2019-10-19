using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
         void RecordLogin(string username);

         Task<IEnumerable<LoginLogEntry>> GetLogins();
    }
}