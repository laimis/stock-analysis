using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.Shared.Adapters.Options;
using core.Shared.Adapters.Stocks;

namespace core.Shared.Adapters.Brokerage
{
    public interface IBrokerage
    {
        Task<string> GetOAuthUrl();
        Task<OAuthResponse> ConnectCallback(string code);
        Task<ServiceResponse<TradingAccount>> GetAccount(UserState state);
        Task<ServiceResponse<bool>> BuyOrder(UserState user, string ticker, decimal numberOfShares, decimal price, BrokerageOrderType type, BrokerageOrderDuration duration);
        Task<ServiceResponse<bool>> SellOrder(UserState user, string ticker, decimal numberOfShares, decimal price, BrokerageOrderType type, BrokerageOrderDuration duration);
        Task<ServiceResponse<bool>> CancelOrder(UserState state, string orderId);
        Task<ServiceResponse<PriceBar[]>> GetPriceHistory(
            UserState state,
            string ticker,
            PriceFrequency frequency = PriceFrequency.Daily,
            DateTimeOffset start = default,
            DateTimeOffset end = default);
        Task<OAuthResponse> GetAccessToken(UserState state);
        Task<ServiceResponse<StockQuote>> GetQuote(UserState state, string ticker);
        Task<ServiceResponse<Dictionary<string, StockQuote>>> GetQuotes(UserState state, IEnumerable<string> tickers);
        Task<ServiceResponse<MarketHours>> GetMarketHours(UserState state, DateTimeOffset start);
        Task<ServiceResponse<SearchResult[]>> Search(UserState state, string query, int limit = 5);
        Task<ServiceResponse<OptionChain>> GetOptions(UserState state, string ticker, DateTimeOffset? expirationDate = null, decimal? strikePrice = null, string contractType = null);
        Task<ServiceResponse<StockProfile>> GetStockProfile(UserState state, string ticker);
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