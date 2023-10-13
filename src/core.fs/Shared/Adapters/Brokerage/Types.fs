namespace core.fs.Shared.Adapters.Brokerage

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Account
open core.Shared
open core.Shared.Adapters.Brokerage
open core.Shared.Adapters.Options
open core.Shared.Adapters.Stocks

type IMarketHours =
    
    abstract member IsMarketOpen : DateTimeOffset -> bool
    abstract member ToMarketTime : DateTimeOffset -> DateTimeOffset
    abstract member ToUniversalTime : DateTimeOffset -> DateTimeOffset
    abstract member GetMarketEndOfDayTimeInUtc : DateTimeOffset -> DateTimeOffset
    abstract member GetMarketStartOfDayTimeInUtc : DateTimeOffset -> DateTimeOffset

type IBrokerage =
    
    abstract member GetOAuthUrl : unit -> Task<string>
    abstract member ConnectCallback : code:string -> Task<OAuthResponse>
    abstract member GetAccount : state:UserState -> Task<ServiceResponse<TradingAccount>>
    abstract member BuyOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<ServiceResponse<bool>>
    abstract member SellOrder : state:UserState -> ticker:Ticker -> numberOfShares:decimal -> price:decimal -> ``type``:BrokerageOrderType -> duration:BrokerageOrderDuration -> Task<ServiceResponse<bool>>
    abstract member CancelOrder : state:UserState -> orderId:string -> Task<ServiceResponse<bool>>
    abstract member GetPriceHistory : state:UserState -> ticker:Ticker -> frequency:PriceFrequency -> start:DateTimeOffset -> ``end``:DateTimeOffset -> Task<ServiceResponse<PriceBar[]>>
    abstract member GetAccessToken : state:UserState -> Task<OAuthResponse>
    abstract member GetQuote : state:UserState -> ticker:Ticker -> Task<ServiceResponse<StockQuote>>
    abstract member GetQuotes : state:UserState -> tickers:Ticker seq -> Task<ServiceResponse<Dictionary<string, StockQuote>>>
    abstract member GetMarketHours : state:UserState -> start:DateTimeOffset -> Task<ServiceResponse<MarketHours>>
    abstract member Search : state:UserState -> query:string -> limit:int -> Task<ServiceResponse<SearchResult[]>>
    abstract member GetOptions : state:UserState -> ticker:Ticker -> expirationDate:DateTimeOffset option -> strikePrice:decimal option -> contractType:string option -> Task<ServiceResponse<OptionChain>>
    abstract member GetStockProfile : state:UserState -> ticker:Ticker -> Task<ServiceResponse<StockProfile>>