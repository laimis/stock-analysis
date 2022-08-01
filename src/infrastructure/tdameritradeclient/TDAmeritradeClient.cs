using core.Shared.Adapters.Brokerage;
using Microsoft.Extensions.Logging;

namespace tdameritradeclient;
public class TDAmeritradeClient : IBrokerage
{
    private ILogger<TDAmeritradeClient> _logger;
    private string _callbackUrl;
    private string _clientId;

    public TDAmeritradeClient(ILogger<TDAmeritradeClient> logger, string callbackUrl, string clientId)
    {
        _logger = logger;
        _callbackUrl = callbackUrl;
        _clientId = clientId;
    }

    public async Task<OAuthResponse?> ConnectCallback(string code)
    {
        // use http client to make x-www-form-urlencoded post request to https://api.tdameritrade.com/v1/oauth2/token
        // with grant_type=authorization_code&code=<code>&redirect_uri=<callbackUrl>&client_id=<clientId>

        var httpClient = new HttpClient();
        var postData = new Dictionary<string, string>
        {
            { "grant_type", "authorization_code" },
            { "code", code },
            { "access_type", "offline" },
            { "redirect_uri", _callbackUrl },
            { "client_id", _clientId }
        };

        var content = new FormUrlEncodedContent(postData);

        var response = await httpClient.PostAsync("https://api.tdameritrade.com/v1/oauth2/token", content);

        var responseString = await response.Content.ReadAsStringAsync();

        _logger.LogDebug("Response from tdameritrade: " + responseString);

        // parse response string into OAuthResponse object
        return System.Text.Json.JsonSerializer.Deserialize<OAuthResponse>(responseString);
    }

    public Task<string> GetOAuthUrl()
    {
        var encodedClientId = Uri.EscapeDataString($"{_clientId}@AMER.OAUTHAP");
        var encodedCallbackUrl = Uri.EscapeDataString(_callbackUrl);
        
        return Task.FromResult($"https://auth.tdameritrade.com/auth?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}");
    }
}
