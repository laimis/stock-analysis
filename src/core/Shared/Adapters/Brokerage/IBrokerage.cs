using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;

namespace core.Shared.Adapters.Brokerage
{
    public interface IBrokerage
    {
        Task<string> GetOAuthUrl();
        Task<OAuthResponse> ConnectCallback(string code);
        Task<IEnumerable<Order>> GetPendingOrders(UserState state);
    }
}