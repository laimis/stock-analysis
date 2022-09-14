namespace tdameritradeclient;

public class HistoricalPriceResponse {
    public Candle[]? candles { get; set; }
}

public class Candle {
    public long datetime { get; set; }
    public decimal open { get; set; }
    public decimal high { get; set; }
    public decimal low { get; set; }
    public decimal close { get; set; }
    public int volume { get; set; }
}

public partial class OrderLeg
{
    public string? orderLegType { get; set; }

    public long legId { get; set; }

    public Instrument? instrument { get; set; }

    public string? instruction { get; set; }

    public string? positionEffect { get; set; }

    public double quantity { get; set; }
}

public class ExecutionLeg
{
    public long legId { get; set; }
    public decimal quantity { get; set; }
    public double mismarkedQuantity { get; set; }
    public decimal price { get; set; }
}

public class OrderActivity
{
    public string? executionType { get; set; }

    public double quantity { get; set; }

    public ExecutionLeg[]? executionLegs { get; set; }
}

public partial class Instrument
{
    public string? assetType { get; set; }

    public string? cusip { get; set; }

    public string? symbol { get; set; }

    public string? description { get; set; }

    public string? type { get; set; }

    public string? putCall { get; set; }

    public string? underlyingSymbol { get; set; }
}

public class OrderStrategy
{
    public string? session { get; set; }

    public string? duration { get; set; }

    public string? orderType { get; set; }

    public string? cancelTime { get; set; }

    public string? complexOrderStrategyType { get; set; }

    public double quantity { get; set; }

    public double filledQuantity { get; set; }

    public double remainingQuantity { get; set; }

    public decimal? stopPrice { get; set; }

    public string? stopType { get; set; }

    public decimal? price { get; set; }

    public string? orderStrategyType { get; set; }

    public long orderId { get; set; }

    public bool cancelable { get; set; }

    public bool editable { get; set; }

    // "CANCELED"
    // "FILLED"
    // "REPLACED"
    // "PENDING_ACTIVATION"
    // "REJECTED"
    // "EXPIRED"
    // "WORKING"
    public string? status { get; set; }

    public string? enteredTime { get; set; }

    public string? closeTime { get; set; }

    public string? tag { get; set; }

    public long accountId { get; set; }

    public string? statusDescription { get; set; }
    
    public OrderLeg[]? orderLegCollection { get; set; }
    public OrderActivity[]? orderActivityCollection { get; set; }

    public bool IsPending => status == "WORKING";

    internal decimal ResolvePrice()
    {
        if (orderActivityCollection != null)
        {
            var executionPrices = orderActivityCollection
                .Where(x => x.executionLegs != null)
                .SelectMany(o => o.executionLegs!)
                .Select(o => o.price);

            if (executionPrices.Any())
            {
                return executionPrices.Average();
            }
        }

        if (price != null)
        {
            return price.Value;
        }
        
        if (stopPrice != null)
        {
            return stopPrice.Value;
        }

        return 0m;
    }
}
public partial class SecuritiesAccount
{
    public string? type { get; set; }

    public string? accountId { get; set; }

    public OrderStrategy[]? orderStrategies { get; set; }
    public TDPosition[]? positions { get; set; }
}

public class TDPosition
{
    public decimal averagePrice { get; set; }
    public decimal longQuantity { get; set; }
    public Instrument? instrument { get; set; }
}

public class AccountsResponse
{
    public SecuritiesAccount? securitiesAccount { get; set; }
}