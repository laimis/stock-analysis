using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Account
{
    public interface IAccountStorage
    {
         void RecordLogin(LoginLogEntry entry);

         Task<IEnumerable<LoginLogEntry>> GetLogins();
    }
}