using System.Threading.Tasks;

namespace core.Shared.Adapters.Cryptos
{
    public interface ICryptoService
    {
         Task<Listings> Get();
    }
}