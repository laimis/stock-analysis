namespace core.Shared.Adapters.Brokerage;

public class OAuthResponse
{
    public string access_token { get; set; }
    public string refresh_token { get; set; }
    public string token_type { get; set; }
    public long expires_in { get; set; }
    public string scope { get; set; }
    public long refresh_token_expires_in { get; set; }
}