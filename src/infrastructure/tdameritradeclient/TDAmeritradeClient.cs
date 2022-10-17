using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using core.Account;
using core.Shared;
using core.Shared.Adapters.Brokerage;
using core.Shared.Adapters.Stocks;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.RateLimit;
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

    public async Task<ServiceResponse<IEnumerable<Order>>> GetOrders(UserState user)
    {
        var response = await CallApi<AccountsResponse[]>(
            user,
            "/accounts?fields=orders",
            HttpMethod.Get);

        if (response.Error != null)
        {
            return new ServiceResponse<IEnumerable<Order>>(response.Error);
        }

        var accounts = response.Success!;

        var strategies = accounts[0].securitiesAccount?.orderStrategies;
        if (strategies == null)
        {
            throw new Exception("Could not find order strategies in response");
        }

        var payload = strategies
            .Select(o => new Order
            {
                Date = o.closeTime == null ? null : DateTimeOffset.Parse(o.closeTime),
                Status = o.status,
                OrderId = o.orderId.ToString(),
                Price = o.ResolvePrice(),
                Quantity = Convert.ToInt32(o.quantity),
                Ticker = o.orderLegCollection?[0]?.instrument?.symbol,
                Type = o.orderLegCollection?[0]?.instruction
            });

        return new ServiceResponse<IEnumerable<Order>>(payload);
    }

    public async Task<ServiceResponse<IEnumerable<Position>>> GetPositions(UserState user)
    {
        var response = await CallApi<AccountsResponse[]>(user, "/accounts?fields=positions", HttpMethod.Get);
        if (response.Error != null)
        {
            return new ServiceResponse<IEnumerable<Position>>(response.Error);
        }

        var accounts = response.Success!;

        var positions = accounts[0].securitiesAccount?.positions;
        if (positions == null)
        {
            throw new Exception("Could not find positions in response");
        }

        var payload = positions.Select(p => new Position
        {
            Ticker = p.instrument?.symbol,
            Quantity = p.longQuantity,
            AverageCost = p.averagePrice
        });

        return new ServiceResponse<IEnumerable<Position>>(payload);
    }

    public async Task<ServiceResponse<bool>> CancelOrder(UserState user, string orderId)
    {
        // get account first
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.Error != null)
        {
            return new ServiceResponse<bool>(response.Error);
        }

        var accounts = response.Success!;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders/{orderId}";

        await CallApiWithoutSerialization(user, url, HttpMethod.Delete);

        return new ServiceResponse<bool>(true);
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

    public async Task<ServiceResponse<HistoricalPrice[]>> GetHistoricalPrices(
        UserState state,
        string ticker,
        PriceFrequency frequency = PriceFrequency.Daily,
        DateTimeOffset start = default,
        DateTimeOffset end = default)
    {
        var startUnix = start == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.AddYears(-2).ToUnixTimeMilliseconds() : start.ToUnixTimeMilliseconds();
        var endUnix = end == DateTimeOffset.MinValue ? DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() : end.ToUnixTimeMilliseconds();

        var frequencyType = frequency switch {
            PriceFrequency.Daily => "daily",
            PriceFrequency.Weekly => "weekly",
            PriceFrequency.Monthly => "monthly",
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };
        
        var function = $"/marketdata/{ticker}/pricehistory?periodType=month&frequencyType={frequencyType}&startDate={startUnix}&endDate={endUnix}";

        var response = await CallApi<HistoricalPriceResponse>(
            state,
            function,
            HttpMethod.Get
        );

        if (response.Error != null)
        {
            return new ServiceResponse<HistoricalPrice[]>(response.Error);
        }

        var prices = response.Success!;

        if (prices.candles == null)
        {
            throw new Exception($"Null candles for historcal prices for {ticker} {start} {end}");
        }

        var payload = prices.candles.Select(c => new HistoricalPrice
        {
            Close = c.close,
            High = c.high,
            Low = c.low,
            Open = c.open,
            Date = DateTimeOffset.FromUnixTimeMilliseconds(c.datetime).ToString("yyyy-MM-dd"),
            Volume = c.volume
        }).ToArray();

        if (payload.Length == 0)
        {
            _logger?.LogError($"No candles for historcal prices for {function}");
        }

        return new ServiceResponse<HistoricalPrice[]>(payload);
    }

    private async Task<ServiceResponse<bool>> EnterOrder(UserState user, object postData)
    {
        var response = await CallApi<AccountsResponse[]>(user, "/accounts", HttpMethod.Get);

        if (response.Error != null)
        {
            return new ServiceResponse<bool>(response.Error);
        }

        var accounts = response.Success!;

        var accountId = accounts[0].securitiesAccount?.accountId;

        var url = $"/accounts/{accountId}/orders";

        var data = JsonSerializer.Serialize(postData);

        _logger?.LogError("Posting to " + url + ": " + data);

        await CallApiWithoutSerialization(user, url, HttpMethod.Post, data);

        return new ServiceResponse<bool>(true);
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

    private async Task<ServiceResponse<T>> CallApi<T>(UserState user, string function, HttpMethod method, string? jsonData = null)
    {
        try
        {
            var responseString = await CallApiWithoutSerialization(user, function, method, jsonData);

            var deserialized = JsonSerializer.Deserialize<T>(responseString);
            if (deserialized == null)
            {
                throw new Exception($"Could not deserialize response for {function}: {responseString}");
            }

            return new ServiceResponse<T>(deserialized);
        }
        catch (TimeoutRejectedException e)
        {
            _logger?.LogError(e, $"Failed to call {function}");

            return new ServiceResponse<T>(new ServiceError(e.Message));
        }
    }

    private AsyncRateLimitPolicy _rateLimit = Policy.RateLimitAsync(40, TimeSpan.FromSeconds(1));

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
        var retryCount = 0;

        while(true)
        {
            try
            {
                var response = await policy.ExecuteAsync(
                    async ct => await _rateLimit.ExecuteAsync(
                        ct2 => _httpClient.SendAsync(request, ct2),
                        ct
                    ),
                    CancellationToken.None
                );

                LogIfFailed(response, function);

                return await response.Content.ReadAsStringAsync();
            }
            catch (RateLimitRejectedException)
            {
                retryCount++;

                if (retryCount > 3)
                {
                    throw;
                }
                else
                {
                    _logger?.LogError($"Rate limit hit for {function}, retrying");
                    await Task.Delay(1000);
                }
            }
        }
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
