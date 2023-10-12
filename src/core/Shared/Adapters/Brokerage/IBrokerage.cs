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
        Task<ServiceResponse<SearchResult[]>> Search(UserState state, string query, int limit);
        Task<ServiceResponse<OptionChain>> GetOptions(UserState state, string ticker, DateTimeOffset? expirationDate = null, decimal? strikePrice = null, string contractType = null);
        Task<ServiceResponse<StockProfile>> GetStockProfile(UserState state, string ticker);
    }

    public struct BrokerageOrderDuration
    {
        public const string Day = nameof(Day);
        public const string Gtc = nameof(Gtc);
        public const string DayPlus = nameof(DayPlus);
        public const string GtcPlus = nameof(GtcPlus);
        
        public BrokerageOrderDuration(string value)
        {
            Value = value switch
            {
                Day => Day,
                Gtc => Gtc,
                DayPlus => DayPlus,
                GtcPlus => GtcPlus,
                _ => throw new ArgumentException("Invalid order duration", nameof(value))
            };
        }

        public string Value { get; }
    }

    public struct BrokerageOrderType
    {
        public const string Limit = nameof(Limit);
        public const string Market = nameof(Market);
        public const string StopMarket = nameof(StopMarket);
        
        public BrokerageOrderType(string value)
        {
            Value = value switch
            {
                Limit => Limit,
                Market => Market,
                StopMarket => StopMarket,
                _ => throw new ArgumentException("Invalid order type", nameof(value))
            };
        }

        public string Value { get; }
    }
}