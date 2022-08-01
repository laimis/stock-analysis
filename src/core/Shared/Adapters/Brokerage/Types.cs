using System;

namespace core.Shared.Adapters.Brokerage;

public class OAuthResponse
{
    private DateTimeOffset _created;

    public OAuthResponse()
    {
        _created = DateTimeOffset.UtcNow;
    }
    
    public string access_token { get; set; }
    public string refresh_token { get; set; }
    public string token_type { get; set; }
    public long expires_in { get; set; }
    public string scope { get; set; }
    public long refresh_token_expires_in { get; set; }
    public bool IsExpired => _created.AddSeconds(expires_in) < DateTimeOffset.UtcNow;
}

public class Order
{
    public string Ticker { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Type { get; set; }
}