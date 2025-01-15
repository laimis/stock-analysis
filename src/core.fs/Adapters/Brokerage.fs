namespace core.fs.Adapters.Brokerage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Account
open core.Shared
open core.fs
open core.fs.Adapters.Options
open core.fs.Adapters.Stocks
open core.fs.Options

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
    
type AssetType =
    | Equity | Option | ETF 
    
[<CLIMutable>]
type SearchResult =
    {
        Symbol:Ticker
        SecurityName:string
        AssetType:AssetType
        Exchange:string
    }
    
type SearchQueryType =
    | Symbol | Description

[<CLIMutable>]
type StockQueryResult =
    {
        Symbol:Ticker
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
        symbol : Ticker
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

type OrderStatus =
    | Filled | Working | PendingActivation | Expired | Canceled | Rejected | Accepted | Replaced
type StockOrderInstruction =
    | Buy | Sell | BuyToCover | SellShort
type OptionOrderInstruction =
    | BuyToOpen | BuyToClose | SellToOpen | SellToClose
type StockOrderType =
    | Market | Limit | StopMarket
type OptionOrderType =
    | Market | Limit | NetDebit | NetCredit

[<CLIMutable>]
type OptionLeg = {
    LegId : string
    Cusip : string
    Ticker : Ticker
    Description: string
    OptionType: OptionType
    UnderlyingTicker : Ticker
    Instruction: OptionOrderInstruction
    Quantity: int
    Price: decimal option
    Expiration: OptionExpiration
    StrikePrice: decimal
}

[<CLIMutable>]
type OptionOrder = {
    OrderId : string
    Price : decimal
    Quantity : decimal
    Status : OrderStatus
    Type : OptionOrderType
    ExecutionTime : DateTimeOffset option
    EnteredTime: DateTimeOffset
    ExpirationTime: DateTimeOffset option
    CanBeCancelled : bool
    Legs : OptionLeg []
} with
        
    member this.IsActive : bool =
        match this.Status with
        | Working -> true
        | PendingActivation -> true
        | _ -> false
    member this.CanBeRecorded : bool = match this.Status with | Filled -> true | _ -> false

[<CLIMutable>]
type StockOrder = {
    OrderId : string
    Price : decimal
    Quantity : decimal
    Status : OrderStatus
    StatusDescription : string option
    Ticker : Ticker
    Type : StockOrderType
    Instruction : StockOrderInstruction
    ExecutionTime : DateTimeOffset option
    EnteredTime: DateTimeOffset
    ExpirationTime: DateTimeOffset option
    CanBeCancelled : bool
    } with
        
    member this.IsActive : bool =
        match this.Status with
        | Working -> true
        | PendingActivation -> true
        | _ -> false
    member this.CanBeRecorded : bool = match this.Status with | Filled -> true | _ -> false
    member this.IsSellOrder : bool = match this.Instruction with | Sell -> true | SellShort -> true | _ -> false
    member this.IsBuyOrder : bool = match this.Instruction with | Buy -> true | BuyToCover -> true | _ -> false
    member this.IsCancelledOrRejected : bool =
        match this.Status with
        | Canceled -> true
        | Rejected -> true
        | Replaced -> true
        | Expired -> true
        | _ -> false 
    member this.IsShort : bool = this.Instruction = SellShort

type AccountTransactionType =
    Trade | Dividend | Interest | Fee | Transfer | Other
    
[<CLIMutable>]
type AccountTransaction = {
    TransactionId : string
    Description : string
    TradeDate: DateTimeOffset
    SettlementDate: DateTimeOffset
    NetAmount: decimal
    BrokerageType: string
    InferredType : AccountTransactionType option
    InferredTicker: Ticker option
    Inserted: DateTimeOffset option
    Applied: DateTimeOffset option
}
    
type BrokerageStockPosition(ticker:Ticker, averageCost:decimal, quantity:decimal) =
    member val Ticker = ticker
    member val AverageCost = averageCost
    member val Quantity = quantity
    
type BrokerageOptionPosition =
    {
        Ticker: Ticker
        OptionType: string
        StrikePrice: decimal
        Quantity: int
        AverageCost: decimal
        MarketValue: decimal option
        ExpirationDate: string
    }
        member this.IsCall = if this.OptionType = "CALL" then "true" else "false"
    
type BrokerageAccount() =
    
    member val StockPositions : BrokerageStockPosition [] = [||] with get, set
    member val OptionPositions : BrokerageOptionPosition [] = [||] with get, set
    member val StockOrders : StockOrder [] = [||] with get, set
    member val OptionOrders : OptionOrder [] = [||] with get, set
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
    | GTC
    | DayPlus
    | GtcPlus
    
    with
        static member FromString (value:string) =
            match value with
            | nameof(Day) -> Day
            | nameof(GTC) -> GTC
            | nameof(DayPlus) -> DayPlus
            | nameof(GtcPlus) -> GtcPlus
            | _ -> failwithf $"Invalid order duration: %s{value}"
            
        override this.ToString() =
            match this with
            | Day -> nameof(Day)
            | GTC -> nameof(GTC)
            | DayPlus -> nameof(DayPlus)
            | GtcPlus -> nameof(GtcPlus)
  
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
    abstract member ConnectCallback : code:string -> Task<Result<OAuthResponse, ServiceError>>
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
    abstract member Search : state:UserState -> searchQueryType:SearchQueryType -> query:string -> limit:int -> Task<Result<SearchResult[],ServiceError>>
    abstract member GetOptionChain : state:UserState -> ticker:Ticker -> Task<Result<OptionChain,ServiceError>>
    abstract member GetStockProfile : state:UserState -> ticker:Ticker -> Task<Result<StockProfile,ServiceError>>
    abstract member GetTransactions : state:UserState -> types:AccountTransactionType array -> Task<Result<AccountTransaction[],ServiceError>>
    abstract member OptionOrder : state:UserState -> payload:string -> Task<Result<unit,ServiceError>>
