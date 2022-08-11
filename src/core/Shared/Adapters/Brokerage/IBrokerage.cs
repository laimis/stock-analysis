using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;

namespace core.Shared.Adapters.Brokerage
{
    public interface IBrokerage
    {
        Task<string> GetOAuthUrl();
        Task<OAuthResponse> ConnectCallback(string code);
        Task<IEnumerable<Order>> GetOrders(UserState state);
        Task<IEnumerable<Position>> GetPositions(UserState state);
        Task BuyOrder(UserState user, string ticker, decimal numberOfShares, decimal price, BrokerageOrderType type, BrokerageOrderDuration duration);
        Task CancelOrder(UserState state, string orderId);
    }

    public enum BrokerageOrderDuration
    {
        Day,
        Gtc,
        DayPlus,
        GtcPlus
    }

    public enum BrokerageOrderType
    {
        Limit,
        Market,
        StopMarket
    }
}