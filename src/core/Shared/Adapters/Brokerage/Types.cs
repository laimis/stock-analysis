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

public class MarketHours
{
    public string category { get; set; }
    public string date { get; set; }
    public string exchange { get; set; }
    public bool isOpen { get; set; }
    public string marketType { get; set; }
    public string product { get; set; }
    public string productName { get; set; }
}

public class StockQuote
{
    public string symbol { get; set; }
    public decimal bidPrice { get; set; }
    public decimal bidSize { get; set; }
    public decimal askPrice { get; set; }
    public decimal askSize { get; set; }
    public decimal lastPrice { get; set; }
    public decimal closePrice { get; set; }
    public decimal lastSize { get; set; }
    public decimal mark { get; set; }
    public string exchange { get; set; }
    public string exchangeName { get; set; }
    public decimal volatility { get; set; }
    public decimal regularMarketLastPrice { get; set; }
    public decimal regularMarketLastSize { get; set; }

    public decimal Price => regularMarketLastPrice;
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