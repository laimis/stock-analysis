namespace di

open System
open System.Collections.Generic
open System.Threading.Tasks
open core.Account
open core.fs
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Stocks
open core.fs.Adapters.Options
open core.Shared

type DummyBrokerageClient() =
    
    interface IBrokerage with
        member _.ConnectCallback(code: string) : Task<Result<OAuthResponse, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetOAuthUrl() : Task<string> =
            raise (NotImplementedException())
        
        member _.GetStockProfile(state: UserState) (ticker: Ticker) : Task<Result<StockProfile, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetTransactions(state: UserState) (types: AccountTransactionType[]) : Task<Result<AccountTransaction[], ServiceError>> =
            raise (NotImplementedException())
        
        member _.OptionOrder(state: UserState) (payload: string) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetAccount(user: UserState) : Task<Result<BrokerageAccount, ServiceError>> =
            raise (NotImplementedException())
        
        member _.CancelOrder(state: UserState) (orderId: string) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.BuyOrder(state: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (orderType: BrokerageOrderType) (duration: BrokerageOrderDuration) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.BuyToCoverOrder(state: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (orderType: BrokerageOrderType) (duration: BrokerageOrderDuration) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.SellShortOrder(state: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (orderType: BrokerageOrderType) (duration: BrokerageOrderDuration) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.SellOrder(state: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (orderType: BrokerageOrderType) (duration: BrokerageOrderDuration) : Task<Result<unit, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetQuote(state: UserState) (ticker: Ticker) : Task<Result<StockQuote, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetQuotes(state: UserState) (tickers: seq<Ticker>) : Task<Result<Dictionary<Ticker, StockQuote>, ServiceError>> =
            raise (NotImplementedException())
        
        member _.Search(state: UserState) (searchQueryType: SearchQueryType) (query: string) (limit: int) : Task<Result<SearchResult[], ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetOptionChain(state: UserState) (source: OptionChainSource) (ticker: Ticker) : Task<Result<OptionChain, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetMarketHours(state: UserState) (date: DateTimeOffset) : Task<Result<MarketHours, ServiceError>> =
            raise (NotImplementedException())
        
        member _.GetPriceHistory(state: UserState) (ticker: Ticker) (frequency: PriceFrequency) (start: DateTimeOffset option) (``end``: DateTimeOffset option) : Task<Result<PriceBars, ServiceError>> =
            raise (NotImplementedException())
        
        member _.RefreshAccessToken(user: UserState) : Task<OAuthResponse> =
            raise (NotImplementedException())
        
        member _.GetAccessToken(user: UserState) : Task<OAuthResponse> =
            raise (NotImplementedException())
