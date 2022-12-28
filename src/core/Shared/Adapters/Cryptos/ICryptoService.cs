using System.Collections.Generic;
using System.Threading.Tasks;

namespace core.Shared.Adapters.Cryptos
{
    public interface ICryptoService
    {
         Task<Listings> Get();

         Task<Price?> Get(string token);

         Task<Dictionary<string, Price>> Get(IEnumerable<string> tokens);
    }
}