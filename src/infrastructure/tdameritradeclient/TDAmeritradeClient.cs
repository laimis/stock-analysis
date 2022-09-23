using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.Account;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Timeout;

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
    private readonly AsyncTimeoutPolicy _timeoutPolicy;

    public TDAmeritradeClient(ILogger<TDAmeritradeClient>? logger, string callbackUrl, string clientId)
    {
        _logger = logger;
        _callbackUrl = callbackUrl;
        _clientId = clientId;
        _httpClient = new HttpClient();
        _timeoutPolicy = Policy.TimeoutAsync((int)TimeSpan.FromSeconds(15).TotalSeconds);
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

        LogIfFailed(response, "connect callback");

        var responseString = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Response from tdameritrade: " + responseString);

        return JsonSerializer.Deserialize<OAuthResponse>(responseString);
    }

    private async void LogIfFailed(HttpResponseMessage response, string message)
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
        try
        {
            var accounts = await CallApi<AccountsResponse[]>(user, "/accounts?fields=orders", HttpMethod.Get);

            var strategies = accounts[0].securitiesAccount?.orderStrategies;
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
        catch (Exception e)
        {
            _logger?.LogError(e, "Failed to get orders");
            return new List<Order>();
        }   
    }

    public async Task<IEnumerable<Position>> GetPositions(UserState user)
    {
        try
        {
            var accounts = await CallApi<AccountsResponse[]>(user, "/accounts?fields=positions", HttpMethod.Get);

            var positions = accounts[0].securitiesAccount?.positions;
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
        catch(TimeoutRejectedException ex)
        {
            _logger?.LogError(ex, "Timeout getting positions");
            return new List<Position>();
        }
        
    }

    public async Task CancelOrder(UserState user, string orderId)
    {
        // get account first
        var accounts = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders/{orderId}";

        await CallApiWithoutSerialization(user, url, HttpMethod.Delete);
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

        var prices = await CallApi<HistoricalPriceResponse>(
            state,
            $"/marketdata/{ticker}/pricehistory?periodType=month&frequencyType=daily&startDate={startUnix}&endDate={endUnix}",
            HttpMethod.Get
        );

        if (prices.candles == null)
        {
            throw new Exception($"Null candles for historcal prices for {ticker} {start} {end}");
        }

        return prices.candles.Select(c => new HistoricalPrice
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
        var accounts = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders";

        var data = JsonSerializer.Serialize(postData);

        _logger?.LogError("Posting to " + url + ": " + data);

        await CallApiWithoutSerialization(user, url, HttpMethod.Post, data);
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

    private async Task<T> CallApi<T>(UserState user, string function, HttpMethod method, string? jsonData = null)
    {
        var responseString = await CallApiWithoutSerialization(user, function, method, jsonData);

        var deserialized = JsonSerializer.Deserialize<T>(responseString);
        if (deserialized == null)
        {
            throw new Exception($"Could not deserialize response for {function}: {responseString}");
        }

        return deserialized;
    }

    private async Task<string> CallApiWithoutSerialization(UserState user, string function, HttpMethod method, string? jsonData = null)
    {
        var accessToken = await GetAccessTokenAsync(user);

        var request = new HttpRequestMessage(method, GenerateApiUrl(function));

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        if (jsonData != null)
        {
            request.Content = new StringContent(jsonData, Encoding.UTF8, "application/json");
        }

        var policy = Policy.TimeoutAsync(30);

        var response = await policy.ExecuteAsync(
            async ct => await _httpClient.SendAsync(request, ct),
            CancellationToken.None
        );

        LogIfFailed(response, function);

        return await response.Content.ReadAsStringAsync();
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

        LogIfFailed(response, "refresh access token");

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
