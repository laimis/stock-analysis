using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Stocks;

namespace core.Shared.Adapters.Brokerage
{
    public interface IBrokerage
    {
        Task<string> GetOAuthUrl();
        Task<OAuthResponse> ConnectCallback(string code);
        Task<ServiceResponse<IEnumerable<Order>>> GetOrders(UserState state);
        Task<ServiceResponse<IEnumerable<Position>>> GetPositions(UserState state);
        Task BuyOrder(UserState user, string ticker, decimal numberOfShares, decimal price, BrokerageOrderType type, BrokerageOrderDuration duration);
        Task SellOrder(UserState user, string ticker, decimal numberOfShares, decimal price, BrokerageOrderType type, BrokerageOrderDuration duration);
        Task<ServiceResponse<bool>> CancelOrder(UserState state, string orderId);
        Task<ServiceResponse<HistoricalPrice[]>> GetHistoricalPrices(
            UserState state,
            string ticker,
            PriceFrequency frequency = PriceFrequency.Daily,
            DateTimeOffset start = default,
            DateTimeOffset end = default);
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