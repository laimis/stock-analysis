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

public class StockQuote
{
    public string symbol { get; set; }
    decimal bidPrice { get; set; }
    decimal bidSize { get; set; }
    decimal askPrice { get; set; }
    decimal askSize { get; set; }
    decimal lastPrice { get; set; }
    decimal lastSize { get; set; }
    decimal mark { get; set; }
    string exchange { get; set; }
    string exchangeName { get; set; }
    decimal volatility { get; set; }
    decimal regularMarketLastPrice { get; set; }
    decimal regularMarketLastSize { get; set; }
}

public class Order
{
    public string OrderId { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }
    public string Ticker { get; set; }
    public string Type { get; set; }
    public DateTimeOffset? Date { get; set; }
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
    public bool IsActive => Status switch
    {
        "WORKING" => true,
        "PENDING_ACTIVATION" => true,
        _ => false
    };

    public bool CanBeRecorded => Status == "FILLED";
    public bool IncludeInResponses => Status != "CANCELED" && Status != "REJECTED" && Status != "EXPIRED";
}

public class Position
{
    public string Ticker { get; set; }
    public decimal AverageCost { get; set; }
    public decimal Quantity { get; set; }
}