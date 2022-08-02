using System.Text.Json;
using core.Account;
using core.Shared.Adapters.Brokerage;
using Microsoft.Extensions.Logging;

namespace tdameritradeclient;
public class TDAmeritradeClient : IBrokerage
{
    private ILogger<TDAmeritradeClient>? _logger;
    private string _callbackUrl;
    private string _clientId;

    // in memory dictionary of access tokens (they expire every 30 mins)
    private Dictionary<Guid, OAuthResponse> _accessTokens = new Dictionary<Guid, OAuthResponse>();

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

        var response = await _httpClient.PostAsync("https://api.tdameritrade.com/v1/oauth2/token", content);

        var responseString = await response.Content.ReadAsStringAsync();

        _logger?.LogDebug("Response from tdameritrade: " + responseString);

        return JsonSerializer.Deserialize<OAuthResponse>(responseString);
    }

    public Task<string> GetOAuthUrl()
    {
        var encodedClientId = Uri.EscapeDataString($"{_clientId}@AMER.OAUTHAP");
        var encodedCallbackUrl = Uri.EscapeDataString(_callbackUrl);
        
        return Task.FromResult($"https://auth.tdameritrade.com/auth?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}");
    }

    public async Task<IEnumerable<Order>> GetPendingOrders(UserState user)
    {
        var accessToken = await GetAccessTokenAsync(user);

        var request = new HttpRequestMessage(HttpMethod.Get, "https://api.tdameritrade.com/v1/accounts?fields=orders");
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", accessToken);

        var response = await _httpClient.SendAsync(request);

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
            .Where(o => o.IsPending)
            .Select(o => new Order {
                Price = o.price.GetValueOrDefault(),
                Quantity = Convert.ToInt32(o.quantity),
                Ticker = o.orderLegCollection?[0]?.instrument?.symbol,
                Type = o.orderLegCollection?[0]?.instruction
            });
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

        var response = await _httpClient.PostAsync("https://api.tdameritrade.com/v1/oauth2/token", content);

        var responseString = await response.Content.ReadAsStringAsync();

        var deserialized = JsonSerializer.Deserialize<OAuthResponse>(responseString);
        if (deserialized == null)
        {
            throw new Exception("Could not deserialize access token: " + responseString);
        }
        return deserialized;
    }
}
