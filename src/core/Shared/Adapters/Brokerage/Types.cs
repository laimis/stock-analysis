using System;

namespace core.Shared.Adapters.Brokerage;

#pragma warning disable IDE1006 // Naming Styles
public class OAuthResponse
{
    private readonly DateTimeOffset _created;

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
    public bool Cancelable { get; set; }
    public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Status { get; set; }
    public string Ticker { get; set; }
    public string Description { get; set; }
    public string Type { get; set; }
    public string AssetType { get; set; }
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
    public bool CanBeCancelled => Cancelable;
    public bool IsActive => Status switch
    {
        "WORKING" => true,
        "PENDING_ACTIVATION" => true,
        _ => false
    };

    public bool CanBeRecorded => Status == "FILLED";
    public bool IncludeInResponses => Status != "CANCELED" && Status != "REJECTED" && Status != "EXPIRED";
    public bool IsSellOrder => Type == "SELL";
    public bool IsBuyOrder => Type == "BUY";
    public bool IsOption => AssetType == "OPTION";
}

public class StockPosition
{
    public string Ticker { get; set; }
    public decimal AverageCost { get; set; }
    public decimal Quantity { get; set; }
}

public class OptionPosition
{
    public string Ticker { get; set; }
    public string OptionType { get; set; }
    public string IsCall => OptionType == "CALL" ? "true" : "false";
    public decimal StrikePrice { get; set; }
    public decimal Quantity { get; set; }
    public decimal AverageCost { get; set; }
    public decimal? MarketValue { get; set; }
    public string ExpirationDate { get; set; }
}

public class TradingAccount
{
    public StockPosition[] StockPositions { get; set; }
    public OptionPosition[] OptionPositions { get; set; }
    public Order[] Orders { get; set; }
    public decimal? CashBalance { get; set; }

    public static readonly TradingAccount Empty = new()
    {
        StockPositions = Array.Empty<StockPosition>(),
        OptionPositions = Array.Empty<OptionPosition>(),
        Orders = Array.Empty<Order>(),
        CashBalance = 0
    };
}

#pragma warning restore IDE1006 // Naming Styles