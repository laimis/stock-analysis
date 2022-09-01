using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;

namespace tdameritradeclient;
public class TDAmeritradeClient : IBrokerage
{
    private ILogger<TDAmeritradeClient>? _logger;
    private string _callbackUrl;
    private string _clientId;

    // in memory dictionary of access tokens (they expire every 30 mins)
    private Dictionary<Guid, OAuthResponse> _accessTokens = new Dictionary<Guid, OAuthResponse>();

    private const string _apiUrl = "https://api.tdameritrade.com/v1";
    private const string _authUrl = "https://auth.tdameritrade.com";

    private HttpClient _httpClient;

    public TDAmeritradeClient(ILogger<TDAmeritradeClient>? logger, string callbackUrl, string clientId)
    {
        _logger = logger;
        _callbackUrl = callbackUrl;
        _clientId = clientId;
        _httpClient = new HttpClient();
    }

    public async Task<OAuthResponse?> ConnectCallback(string code)
    {
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "access_type", "offline" },
            { "redirect_uri", _callbackUrl },
            { "client_id", _clientId }
        };

        var content = new FormUrlEncodedContent(postData);

        var response = await _httpClient.PostAsync(GenerateApiUrl("/oauth2/token"), content);

        LogAndThrowIfFailed(response, "connect callback");

        var responseString = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Response from tdameritrade: " + responseString);

        return JsonSerializer.Deserialize<OAuthResponse>(responseString);
    }

    private async void LogAndThrowIfFailed(HttpResponseMessage response, string message)
    {
        if (!response.IsSuccessStatusCode)
        {
            var content = await response.Content.ReadAsStringAsync();
            _logger?.LogError("TDAmeritrade client failed at " + message + ": " + content);
        }
    }

    public Task<string> GetOAuthUrl()
    {
        var encodedClientId = Uri.EscapeDataString($"{_clientId}@AMER.OAUTHAP");
        var encodedCallbackUrl = Uri.EscapeDataString(_callbackUrl);
        var url = $"{_authUrl}/auth?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}";
        
        return Task.FromResult(url);
    }

    public async Task<IEnumerable<Order>> GetOrders(UserState user)
    {
        var response = await CallApi(user, "/accounts?fields=orders", HttpMethod.Get);

        LogAndThrowIfFailed(response, "get pending orders");

        var responseString = await response.Content.ReadAsStringAsync();

        // parse response string into Order objects
        var deserialized = JsonSerializer.Deserialize<AccountsResponse[]>(responseString);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize orders: " + responseString);
        }

        var strategies = deserialized[0].securitiesAccount?.orderStrategies;
        if (strategies == null)
        {
            throw new Exception("Could not find order strategies in response");
        }

        return strategies
            .Select(o => new Order
            {
                Status = o.status,
                OrderId = o.orderId.ToString(),
                Price = o.ResolvePrice(),
                Quantity = Convert.ToInt32(o.quantity),
                Ticker = o.orderLegCollection?[0]?.instrument?.symbol,
                Type = o.orderLegCollection?[0]?.instruction
            });
    }

    public async Task<IEnumerable<Position>> GetPositions(UserState user)
    {
        var request = await CallApi(user, "/accounts?fields=positions", HttpMethod.Get);

        LogAndThrowIfFailed(request, "get positions");

        var responseString = await request.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<AccountsResponse[]>(responseString);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize positions: " + responseString);
        }

        var positions = deserialized[0].securitiesAccount?.positions;
        if (positions == null)
        {
            throw new Exception("Could not find positions in response");
        }

        return positions.Select(p => new Position
        {
            Ticker = p.instrument?.symbol,
            Quantity = p.longQuantity,
            AverageCost = p.averagePrice
        });
    }

    public async Task CancelOrder(UserState user, string orderId)
    {
        // get account first
        var accounts = await CallApi(user, "/accounts", HttpMethod.Get);

        LogAndThrowIfFailed(accounts, "get account for cancel order");

        var account = await accounts.Content.ReadAsStringAsync();
        var deserialized = JsonSerializer.Deserialize<AccountsResponse[]>(account);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize account: " + account);
        }

        var accountId = deserialized[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders/{orderId}";

        var response = await CallApi(user, url, HttpMethod.Delete);

        LogAndThrowIfFailed(response, "cancel order");
    }

    public Task BuyOrder(
        UserState user,
        string ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "Buy",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = price,
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        return EnterOrder(user, postData);
    }

    public Task SellOrder(
        UserState user,
        string ticker,
        decimal numberOfShares,
        decimal price,
        BrokerageOrderType type,
        BrokerageOrderDuration duration)
    {
        var legCollection = new {
            instruction = "Sell",
            quantity = numberOfShares,
            instrument = new {
                symbol = ticker,
                assetType = "EQUITY"
            }
        };

        var postData = new
        {
            orderType = GetBuyOrderType(type),
            session = GetSession(duration),
            duration = GetBuyOrderDuration(duration),
            price = price,
            orderStrategyType = "SINGLE",
            orderLegCollection = new [] {legCollection}
        };

        // get account first
        return EnterOrder(user, postData);
    }

    public async Task<HistoricalPrice[]> GetHistoricalPrices(UserState state, string ticker, DateTimeOffset start = default, DateTimeOffset end = default)
    {
        var startUnix = start == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.AddYears(-2).ToUnixTimeMilliseconds() : start.ToUnixTimeMilliseconds();
        var endUnix = end == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : end.ToUnixTimeMilliseconds();

        var response = await CallApi(state, $"/marketdata/{ticker}/pricehistory?periodType=month&frequencyType=daily&startDate={startUnix}&endDate={endUnix}", HttpMethod.Get);

        LogAndThrowIfFailed(response, "get historical prices");

        var responseString = await response.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<HistoricalPriceResponse>(responseString);
        if (deserialized == null || deserialized.candles == null)
        {
            throw new Exception("Could not deserialize historical prices: " + responseString);
        }

        return deserialized.candles.Select(c => new HistoricalPrice
        {
            Close = c.close,
            High = c.high,
            Low = c.low,
            Open = c.open,
            Date = DateTimeOffset.FromUnixTimeMilliseconds(c.datetime).ToString("yyyy-MM-dd"),
            Volume = c.volume
        }).ToArray();
    }

    private async Task EnterOrder(UserState user, object postData)
    {
        var accounts = await CallApi(user, "/accounts", HttpMethod.Get);

        LogAndThrowIfFailed(accounts, "get account");

        var account = await accounts.Content.ReadAsStringAsync();
        var deserialized = JsonSerializer.Deserialize<AccountsResponse[]>(account);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize account: " + account);
        }

        var accountId = deserialized[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders";

        var data = JsonSerializer.Serialize(postData);

        _logger?.LogError("Posting to " + url + ": " + data);

        var response = await CallApi(user, url, HttpMethod.Post, data);

        LogAndThrowIfFailed(response, "brokerage order");
    }

    private string GetBuyOrderType(BrokerageOrderType type) =>
        type switch {
            BrokerageOrderType.Limit => "LIMIT",
            BrokerageOrderType.Market => "MARKET",
            BrokerageOrderType.StopMarket => "STOP",
            _ => throw new ArgumentException("Unknown order type: " + type)
        };

    private string GetBuyOrderDuration(BrokerageOrderDuration duration) =>
        duration switch {
            BrokerageOrderDuration.Day => "DAY",
            BrokerageOrderDuration.Gtc => "GOOD_TILL_CANCEL",
            BrokerageOrderDuration.DayPlus => "DAY",
            BrokerageOrderDuration.GtcPlus => "GOOD_TILL_CANCEL",
            _ => throw new ArgumentException("Unknown order type: " + duration)
        };

    private string GetSession(BrokerageOrderDuration duration) =>
        duration switch {
            BrokerageOrderDuration.Day => "NORMAL",
            BrokerageOrderDuration.Gtc => "NORMAL",
            BrokerageOrderDuration.DayPlus => "SEAMLESS",
            BrokerageOrderDuration.GtcPlus => "SEAMLESS",
            _ => throw new ArgumentException("Unknown order type: " + duration)
        };

    private async Task<HttpResponseMessage> CallApi(UserState user, string function, HttpMethod method, string? jsonData = null)
    {
        var accessToken = await GetAccessTokenAsync(user);

        var request = new HttpRequestMessage(method, GenerateApiUrl(function));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (jsonData != null)
        {
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        }

        return await _httpClient.SendAsync(request);
    }

    private async Task<string> GetAccessTokenAsync(UserState user)
    {
        if (!_accessTokens.ContainsKey(user.Id) || _accessTokens[user.Id].IsExpired)
        {
            _accessTokens[user.Id] = await RefreshAccessToken(user);
        }
        return _accessTokens[user.Id].access_token;
    }

    private async Task<OAuthResponse> RefreshAccessToken(UserState user)
    {
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "refresh_token" },
            { "refresh_token", user.BrokerageRefreshToken },
            { "client_id", _clientId }
        };

        var content = new FormUrlEncodedContent(postData);

        var response = await _httpClient.PostAsync(GenerateApiUrl("/oauth2/token"), content);

        LogAndThrowIfFailed(response, "refresh access token");

        var responseString = await response.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<OAuthResponse>(responseString);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize access token: " + responseString);
        }
        return deserialized;
    }

    private string GenerateApiUrl(string function) => $"{_apiUrl}/{function}";
}
