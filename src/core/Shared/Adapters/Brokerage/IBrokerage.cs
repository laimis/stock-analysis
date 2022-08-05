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
        Task<IEnumerable<Position>> GetPositions(UserState state);
        Task BuyOrder(UserState user, string ticker, decimal numberOfShares, decimal price, string type);
        Task CancelOrder(UserState state, string orderId);
    }
}