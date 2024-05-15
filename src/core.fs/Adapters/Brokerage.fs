namespace core.fs.Adapters.Brokerage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Account
open core.Shared
open core.fs
open core.fs.Adapters.Options
open core.fs.Adapters.Stocks

type OAuthResponse() =
    
    member val access_token : string = "" with get, set
    member val refresh_token : string = "" with get, set
    member val token_type : string = "" with get, set
    member val expires_in : int64 = 0L with get, set
    member val scope : string = "" with get, set
    member val refresh_token_expires_in : int64 = 0L with get, set
    member val error : string = "" with get, set
    member val created : DateTimeOffset option = None with get, set
    member this.IsExpired : bool =
        this.created = None || this.created.Value.AddSeconds(this.expires_in |> float) < DateTimeOffset.UtcNow
    member this.IsError : bool = this.error <> ""

type MarketHours() =
    
    member val category : string = "" with get, set
    member val date : string = "" with get, set
    member val exchange : string = "" with get, set
    member val isOpen : bool = false with get, set
    member val marketType : string = "" with get, set
    member val product : string = "" with get, set
    member val productName : string = "" with get, set
    
[<CLIMutable>]
type SearchResult =
    {
        Symbol:string
        SecurityName:string
        AssetType:string
        Region:string
        Exchange:string
    }       

[<CLIMutable>]
type StockQueryResult =
    {
        Symbol:string
        CompanyName:string
        LatestPrice:decimal
        LatestSource:string
        LatestTime:string
        MarketCap:int64
        Volume:int64
        Week52High:decimal
        Week52Low:decimal
        PERatio:decimal option
    }

[<CLIMutable>]
type StockQuote =
    {
        symbol : string
        bidPrice : decimal
        bidSize : decimal
        askPrice : decimal
        askSize : decimal
        lastPrice : decimal
        closePrice : decimal
        lastSize : decimal
        mark : decimal
        exchange : string
        exchangeName : string
        volatility : decimal
        regularMarketLastPrice : decimal
    }
    member this.Price : decimal = this.regularMarketLastPrice

type Order() =
    
    member val OrderId : string = "" with get, set
    member val Cancelable : bool = false with get, set
    member val Price : decimal = 0m with get, set
    member val Quantity : int = 0 with get, set
    member val Status : string = "" with get, set
    member val Ticker : Ticker option = None with get, set
    member val Description : string = "" with get, set
    member val Type : string = "" with get, set
    member val AssetType : string = "" with get, set
    member val Date : DateTimeOffset option = None with get, set
    
    member this.StatusOrder : int =
        match this.Status with
        | "WORKING" -> 0
        | "PENDING_ACTIVATION" -> 0
        | "FILLED" -> 1
        | "EXPIRED" -> 2
        | "CANCELED" -> 3
        | _ -> 4
    member this.CanBeCancelled : bool = this.Cancelable
    member this.IsActive : bool =
        match this.Status with
        | "WORKING" -> true
        | "PENDING_ACTIVATION" -> true
        | _ -> false
    member this.CanBeRecorded : bool = this.Status = "FILLED"
    member this.IncludeInResponses : bool = this.Status <> "CANCELED" && this.Status <> "REJECTED" && this.Status <> "EXPIRED"
    member this.IsSellOrder : bool = this.Type = "SELL" || this.Type = "SELL_SHORT"
    member this.IsBuyOrder : bool = this.Type = "BUY" || this.Type = "BUY_TO_COVER"
    member this.IsOption : bool = this.AssetType = "OPTION"
    member this.IsShort : bool = this.Type = "SELL_SHORT"
    
    
type StockPosition(ticker:Ticker, averageCost:decimal, quantity:decimal) =
    member val Ticker = ticker
    member val AverageCost = averageCost
    member val Quantity = quantity
    
type OptionPosition() =
    
    member val Ticker : Ticker option = None with get, set
    member val OptionType : string = "" with get, set
    member val StrikePrice : decimal = 0m with get, set
    member val Quantity : decimal = 0m with get, set
    member val AverageCost : decimal = 0m with get, set
    member val MarketValue : decimal option = None with get, set
    member val ExpirationDate : string = "" with get, set
    
    member this.IsCall = if this.OptionType = "CALL" then "true" else "false"
    
type BrokerageAccount() =
    
    member val StockPositions : StockPosition [] = [||] with get, set
    member val OptionPositions : OptionPosition [] = [||] with get, set
    member val Orders : Order [] = [||] with get, set
    member val CashBalance : decimal option = None with get, set
    member val Equity : decimal option = None with get, set
    member val LongMarketValue : decimal option = None with get, set
    member val ShortMarketValue : decimal option = None with get, set
    member val Connected : bool = true with get, set
    
    static member Empty : BrokerageAccount =
        let trading:BrokerageAccount = BrokerageAccount()
        trading.CashBalance <- Some 0m
        trading.Connected <- false
        trading
    
type BrokerageOrderDuration =
    | Day
    | Gtc
    | DayPlus
    | GtcPlus
    
    with
        static member FromString (value:string) =
            match value with
            | nameof(Day) -> Day
            | nameof(Gtc) -> Gtc
            | nameof(DayPlus) -> DayPlus
            | nameof(GtcPlus) -> GtcPlus
            | _ -> failwithf $"Invalid order duration: %s{value}"
            
        override this.ToString() =
            match this with
            | Day -> "Day"
            | Gtc -> "Gtc"
            | DayPlus -> "DayPlus"
            | GtcPlus -> "GtcPlus"
  
type BrokerageOrderType =
    | Limit
    | Market
    | StopMarket
    
    with
        static member FromString(value:string) =
            match value with
            | nameof Limit -> Limit
            | nameof Market -> Market
            | nameof StopMarket -> StopMarket
            | _ -> failwithf $"Invalid order type: %s{value}"
            
        override this.ToString() =
            match this with
            | Limit -> "Limit"
            | Market -> "Market"
            | StopMarket -> "StopMarket"
            
type IMarketHours =
    
    abstract member IsMarketOpen : DateTimeOffset -> bool
    abstract member ToMarketTime : DateTimeOffset -> DateTimeOffset
    abstract member ToUniversalTime : DateTimeOffset -> DateTimeOffset
    abstract member GetMarketEndOfDayTimeInUtc : DateTimeOffset -> DateTimeOffset
    abstract member GetMarketStartOfDayTimeInUtc : DateTimeOffset -> DateTimeOffset

type IBrokerageGetPriceHistory =
    
    abstract member GetPriceHistory : state:UserState -> ticker:Ticker -> frequency:PriceFrequency -> start:DateTimeOffset option -> ``end``:DateTimeOffset option -> Task<Result<PriceBars,ServiceError>>
    
type IBrokerage =
    inherit IBrokerageGetPriceHistory
    abstract member GetOAuthUrl : unit -> Task<string>
    abstract member ConnectCallback : code:string -> Task<OAuthResponse>
    abstract member GetAccessToken : state:UserState -> Task<OAuthResponse>
    abstract member RefreshAccessToken : state:UserState -> Task<OAuthResponse>
    abstract member GetAccount : state:UserState -> Task<Result<BrokerageAccount,ServiceError>>
    abstract member BuyOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<Result<unit,ServiceError>>
    abstract member BuyToCoverOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<Result<unit,ServiceError>>
    abstract member SellOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<Result<unit,ServiceError>>
    abstract member SellShortOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<Result<unit,ServiceError>>
    abstract member CancelOrder : state:UserState -> orderId:string -> Task<Result<unit,ServiceError>>
    abstract member GetQuote : state:UserState -> ticker:Ticker -> Task<Result<StockQuote,ServiceError>>
    abstract member GetQuotes : state:UserState -> tickers:Ticker seq -> Task<Result<Dictionary<Ticker, StockQuote>,ServiceError>>
    abstract member GetMarketHours : state:UserState -> start:DateTimeOffset -> Task<Result<MarketHours,ServiceError>>
    abstract member Search : state:UserState -> query:string -> limit:int -> Task<Result<SearchResult[],ServiceError>>
    abstract member GetOptions : state:UserState -> ticker:Ticker -> expirationDate:DateTimeOffset option -> strikePrice:decimal option -> contractType:string option -> Task<Result<OptionChain,ServiceError>>
    abstract member GetStockProfile : state:UserState -> ticker:Ticker -> Task<Result<StockProfile,ServiceError>>
