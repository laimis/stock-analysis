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
    public string OrderId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }
    public string Ticker { get; set; }
    public string Type { get; set; }
    public int StatusOrder => Status switch
    {
        "WORKING" => 0,
        "PENDING_ACTIVATION" => 0,
        "FILLED" => 1,
        "EXPIRED" => 2,
        "CANCELED" => 3,
        _ => 4
    };
    public bool CanBeCancelled => Status switch
    {
        "WORKING" => true,
        "PENDING_ACTIVATION" => true,
        _ => false
    };
    public bool IncludeInResponses => Status != "CANCELED" && Status != "REJECTED" && Status != "EXPIRED";
}

public class Position
{
    public string Ticker { get; set; }
    public decimal AveragePrice { get; set; }
    public decimal Quantity { get; set; }
}