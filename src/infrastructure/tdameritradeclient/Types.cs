using core.Shared.Adapters.Brokerage;

namespace tdameritradeclient;


internal class ErrorResponse
{
    public string? error { get; set; }
}

internal class MarketHoursWrapper
{
    public MarketHoursEquity? equity { get; set; }
}
internal class MarketHoursEquity
{
    public MarketHours? EQ { get; set; }
}

internal class PriceHistoryResponse {
    public Candle[]? candles { get; set; }
}

internal class Candle {
    public long datetime { get; set; }
    public decimal open { get; set; }
    public decimal high { get; set; }
    public decimal low { get; set; }
    public decimal close { get; set; }
    public int volume { get; set; }
}

internal partial class OrderLeg
{
    public string? orderLegType { get; set; }

    public long legId { get; set; }

    public Instrument? instrument { get; set; }

    public string? instruction { get; set; }

    public string? positionEffect { get; set; }

    public double quantity { get; set; }
}

internal class ExecutionLeg
{
    public long legId { get; set; }
    public decimal quantity { get; set; }
    public double mismarkedQuantity { get; set; }
    public decimal price { get; set; }
}

internal class OrderActivity
{
    public string? executionType { get; set; }

    public double quantity { get; set; }

    public ExecutionLeg[]? executionLegs { get; set; }
}

internal class Instrument
{
    public string? assetType { get; set; }

    public string? cusip { get; set; }

    public string? symbol { get; set; }

    public string? description { get; set; }

    public string? type { get; set; }

    public string? putCall { get; set; }

    public string? underlyingSymbol { get; set; }
}

internal class OrderStrategy
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
internal class SecuritiesAccount
{
    public string? type { get; set; }

    public string? accountId { get; set; }

    public OrderStrategy[]? orderStrategies { get; set; }
    public TDPosition[]? positions { get; set; }
}

internal class TDPosition
{
    public decimal averagePrice { get; set; }
    public decimal longQuantity { get; set; }
    public Instrument? instrument { get; set; }
}

internal class AccountsResponse
{
    public SecuritiesAccount? securitiesAccount { get; set; }
}

internal class SearchItem
{
    public string? cusip { get; set; }
    public string? symbol { get; set; }
    public string? description { get; set; }
    public string? exchange { get; set; }
    public string? assetType { get; set; }
}

internal class SearchItemWithFundamental : SearchItem
{
    // I have a concrete type with all the properties that are returned
    // but don't have the need for static typing here
    // and just looking to output all the properties
    // that the API gives
    public Dictionary<string, object>? fundamental { get; set; }
}

internal class Fundamental
{
    public string? symbol { get; set; }

    public double high52 { get; set; }

    public double low52 { get; set; }

    public double dividendAmount { get; set; }

    public double dividendYield { get; set; }

    public string? dividendDate { get; set; }

    public double peRatio { get; set; }

    public double pegRatio { get; set; }

    public double pbRatio { get; set; }

    public double prRatio { get; set; }

    public double pcfRatio { get; set; }

    public double grossMarginTtm { get; set; }

    public double grossMarginMrq { get; set; }

    public double netProfitMarginTtm { get; set; }

    public double netProfitMarginMrq { get; set; }

    public double operatingMarginTtm { get; set; }

    public double operatingMarginMrq { get; set; }

    public double returnOnEquity { get; set; }

    public double returnOnAssets { get; set; }

    public double returnOnInvestment { get; set; }

    public double quickRatio { get; set; }

    public double currentRatio { get; set; }

    public double interestCoverage { get; set; }

    public double totalDebtToCapital { get; set; }

    public double ltDebtToEquity { get; set; }

    public double totalDebtToEquity { get; set; }

    public double epsTtm { get; set; }

    public double epsChangePercentTtm { get; set; }

    public double epsChangeYear { get; set; }

    public double epsChange { get; set; }

    public double revChangeYear { get; set; }

    public double revChangeTtm { get; set; }

    public double revChangeIn { get; set; }

    public double sharesOutstanding { get; set; }

    public double marketCapFloat { get; set; }

    public double marketCap { get; set; }

    public double bookValuePerShare { get; set; }

    public double shortIntToFloat { get; set; }

    public double shortIntDayToCover { get; set; }

    public double divGrowthRate3Year { get; set; }

    public double dividendPayAmount { get; set; }

    public string? dividendPayDate { get; set; }

    public double beta { get; set; }

    public double vol1DayAvg { get; set; }

    public double vol10DayAvg { get; set; }

    public double vol3MonthAvg { get; set; }
}

internal class OptionDescriptor
{
    public string? putCall { get; set; }
    public string? symbol { get; set; }
    public string? description { get; set; }
    public string? exchangeName { get; set; }
    public decimal bid { get; set; }
    public decimal ask { get; set; }
    public decimal last { get; set; }
    public decimal mark { get; set; }
    public int bidSize { get; set; }
    public int askSize { get; set; }
    public decimal highPrice { get; set; }
    public decimal lowPrice { get; set; }
    public int totalVolume { get; set; }
    public decimal volatility { get; set; }
    public decimal delta { get; set; }
    public decimal gamma { get; set; }
    public decimal theta { get; set; }
    public decimal vega { get; set; }
    public decimal rho { get; set; }
    public long openInterest { get; set; }
    public decimal timeValue { get; set; }
    public long expirationDate { get; set; }
    public DateTimeOffset ExpirationDate => DateTimeOffset.FromUnixTimeMilliseconds(expirationDate);
    public int daysToExpiration { get; set; }
    public decimal percentChange { get; set; }
    public decimal markChange { get; set; }
    public decimal markPercentChange { get; set; }
    public decimal intrinsicValue { get; set; }
    public bool inTheMoney { get; set; }
    public decimal strikePrice { get; set; }
}

internal class OptionDescriptorMap : Dictionary<string, OptionDescriptor[]>
{
    public OptionDescriptor First(string key) => this[key].First();
}


internal class OptionChain
{
    public string? symbol { get; set; }
    public string? status { get; set; }
    public decimal volatility { get; set; }
    public int numberOfContracts { get; set; }
    public Dictionary<string, OptionDescriptorMap>? putExpDateMap { get; set; }
    public Dictionary<string, OptionDescriptorMap>? callExpDateMap { get; set; }
}