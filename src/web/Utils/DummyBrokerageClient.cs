using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using core.fs;
using core.fs.Adapters.Brokerage;
using core.fs.Adapters.Stocks;
using core.Shared;
using Microsoft.FSharp.Core;
using OptionChain = core.fs.Adapters.Options.OptionChain;

namespace web.Utils;

public class DummyBrokerageClient : IBrokerage
{
    public Task<OAuthResponse> ConnectCallback(string code)
    {
        throw new NotImplementedException();
    }

    public Task<string> GetOAuthUrl()
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<StockProfile,ServiceError>> GetStockProfile(UserState state, Ticker ticker)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<SearchResult[], ServiceError>> Search(UserState state, string query, int limit)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<BrokerageAccount,ServiceError>> GetAccount(UserState user)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<Unit,ServiceError>> CancelOrder(UserState user, string orderId)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<Unit,ServiceError>> BuyOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        throw new NotImplementedException();
    }
    
    public Task<FSharpResult<Unit,ServiceError>> BuyToCoverOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        throw new NotImplementedException();
    }
    
    public Task<FSharpResult<Unit,ServiceError>> SellShortOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<Unit,ServiceError>> SellOrder(
        UserState user,
        Ticker ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<StockQuote,ServiceError>> GetQuote(UserState user, Ticker ticker)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<Dictionary<Ticker, StockQuote>, ServiceError>> GetQuotes(UserState user, IEnumerable<Ticker> tickers)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<OptionChain, ServiceError>> GetOptions(UserState state, Ticker ticker, FSharpOption<DateTimeOffset> expirationDate, FSharpOption<decimal> strikePrice, FSharpOption<string> contractType)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<MarketHours, ServiceError>> GetMarketHours(UserState state, DateTimeOffset date)
    {
        throw new NotImplementedException();
    }

    public Task<FSharpResult<PriceBars, ServiceError>> GetPriceHistory(
        UserState state,
        Ticker ticker,
        PriceFrequency frequency,
        FSharpOption<DateTimeOffset> start,
        FSharpOption<DateTimeOffset> end)
    {
        throw new NotImplementedException();
    }

    public Task<OAuthResponse> RefreshAccessToken(UserState user) =>
        throw new NotImplementedException();

    public Task<OAuthResponse> GetAccessToken(UserState user)
    {
        throw new NotImplementedException();
    }
}
