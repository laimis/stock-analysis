namespace SchwabClient

open System
open System.Collections.Generic
open System.Globalization
open System.Net.Http
open System.Net.Http.Headers
open System.Text
open System.Text.Json
open System.Text.Json.Serialization
open System.Threading
open System.Threading.Tasks
open Polly.Timeout
open core.fs
open core.fs.Adapters.Brokerage
open core.Account
open core.Shared
open Microsoft.Extensions.Logging
open Microsoft.FSharp.Core
open Nito.AsyncEx
open Polly
open Polly.RateLimit
open core.fs.Adapters.Stocks
open core.fs.Options
open schwabclient
    
type ErrorResponse = {
    message: string option
}

type MarketHoursEquity = {
    EQ: MarketHours option
}

type MarketHoursWrapper = {
    equity: MarketHoursEquity option
}

type Candle = {
    datetime: int64
    ``open``: decimal
    high: decimal
    low: decimal
    close: decimal
    volume: int
}

type PriceHistoryResponse = {
    candles: Candle [] option
}

[<CLIMutable>]
type Instrument = {
    assetType: string
    cusip: string
    symbol: string
    description: string
    underlyingSymbol: string
    putCall: string
}

[<CLIMutable>]
type OrderLeg = {
    orderLegType: string
    legId: int64
    cusip: string
    instrument: Instrument
    instruction: string
    positionEffect: string
    quantity: decimal
}

type ExecutionLeg = {
    legId: int64
    quantity: decimal
    mismarkedQuantity: float
    price: decimal
}

type OrderActivity = {
    executionType: string option
    quantity: float
    executionLegs: ExecutionLeg [] option
}

[<CLIMutable>]
type OrderStrategy = {
    session: string
    duration: string
    orderType: string
    cancelTime: string option
    quantity: float
    filledQuantity: float
    remainingQuantity: float
    stopPrice: decimal option
    stopType: string option
    price: decimal option
    orderId: int64
    cancelable: bool
    editable: bool
    status: string
    statusDescription: string option
    enteredTime: string
    closeTime: string option
    tag: string
    orderLegCollection: OrderLeg [] option
    orderActivityCollection: OrderActivity [] option
}
    with
    
        
        member this.ResolveStockPrice() =
            let firstPass =
                match this.orderActivityCollection with
                | Some activities ->
                    let executionPrices =
                        activities
                        |> Array.choose (fun o ->
                            match o.executionLegs with
                            | Some legs -> Some(legs |> Array.map(_.price))
                            | None -> None
                        )
                        |> Array.concat
                    
                    if not (Array.isEmpty executionPrices) then
                        Array.average executionPrices
                    else
                        match this.price with
                        | Some p -> p
                        | None ->
                            match this.stopPrice with
                            | Some sp -> sp
                            | None -> 0m
                | None ->
                    match this.price with
                    | Some p -> p
                    | None ->
                        match this.stopPrice with
                        | Some sp -> sp
                        | None -> 0m
                        
            // first pass will prefer order activity collection resolution
            // but in case the order was cancelled, the activity collection has 0 as price
            // while the order itself has the correct price, so fall back to that
            match firstPass with
            | 0m -> this.price |> Option.defaultValue 0m
            | _ -> firstPass
                    
        member this.ResolveOptionPrice() =
            match this.price with
            | Some p -> p
            | None -> 0m
            
        member this.ResolveOptionLegPrice(legId:int64) =
            match this.orderActivityCollection with
            | None -> None
            | Some ac ->
                let leg = ac |> Array.tryFind(fun a -> a.executionLegs |> Option.defaultValue [||] |> Array.exists(fun l -> l.legId = legId))
                match leg with
                | Some l -> l.executionLegs |> Option.defaultValue [||] |> Array.tryFind(fun l -> l.legId = legId) |> Option.map(_.price)
                | None -> None

type BrokerageBalances = {
    cashBalance: decimal
    equity: decimal
    longMarketValue: decimal
    shortMarketValue: decimal
}

type BrokeragePosition = {
    averagePrice: decimal
    longQuantity: decimal
    shortQuantity: decimal
    instrument: Instrument option
    marketValue: decimal option
}

type SecuritiesAccount = {
    type': string option
    accountId: string option
    positions: BrokeragePosition [] option
    currentBalances: BrokerageBalances option
}

type AccountsResponse = {
    securitiesAccount: SecuritiesAccount option
}

[<CLIMutable>]
type AccountNumber = {
    accountNumber: string
    hashValue: string
}

[<CLIMutable>]
type SearchItem = {
    cusip: string
    symbol: string
    description: string
    exchange: string
    assetType: string
    fundamental: Dictionary<string, obj> option
}

type InstrumentsResponse = {
    instruments: SearchItem [] option
}

type TransactionItem = {
    activityId: int64
    time: string
    description: string
    ``type``: string
    status: string
    tradeDate: string
    settlementDate: string
    netAmount: decimal
    activityType: string
}

type RegularMarketQuote = {
    regularMarketLastPrice: decimal
}

[<CLIMutable>]
type SchwabStockQuote = {
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
}

type SchwabStockQuoteResponse = {
    quote: SchwabStockQuote
    regular: RegularMarketQuote
}

type OptionDescriptor = {
    putCall: string option
    symbol: string option
    description: string option
    exchangeName: string option
    bid: decimal
    ask: decimal
    last: decimal
    mark: decimal
    bidSize: int
    askSize: int
    highPrice: decimal
    lowPrice: decimal
    totalVolume: int
    volatility: decimal
    delta: decimal
    gamma: decimal
    theta: decimal
    vega: decimal
    rho: decimal
    openInterest: int64
    timeValue: decimal
    expirationDate: string
    daysToExpiration: int
    percentChange: decimal
    markChange: decimal
    markPercentChange: decimal
    intrinsicValue: decimal
    inTheMoney: bool
    strikePrice: decimal
}

type OptionChain = {
    symbol: string option
    status: string option
    volatility: decimal
    numberOfContracts: int
    underlyingPrice: decimal option
    putExpDateMap: Dictionary<string, Dictionary<string, OptionDescriptor[]>>
    callExpDateMap: Dictionary<string, Dictionary<string, OptionDescriptor[]>>
}

type NanConverter() =
    inherit JsonConverter<decimal>()

    override this.Read(reader: byref<Utf8JsonReader>, _, _) =
        if reader.TokenType = JsonTokenType.String then
            let stringValue = reader.GetString()
            if stringValue = "NaN" then
                0m
            else
                reader.GetDecimal()
        else
            reader.GetDecimal()

    override this.Write(writer: Utf8JsonWriter, value: decimal, _) =
        writer.WriteNumberValue(value)

type SchwabResourceEndpoint =
    | AuthAUrl of string
    | TraderApiUrl of string
    | MarketDataUrl of string

type SchwabClient(blobStorage: IBlobStorage, callbackUrl: string, clientId: string, clientSecret:string, logger: ILogger<SchwabClient> option) =
    
    do
        // make sure callback url, client id, and client secret are set
        if String.IsNullOrWhiteSpace(callbackUrl) then
            raise (ArgumentNullException("callbackUrl"))
        if String.IsNullOrWhiteSpace(clientId) then
            raise (ArgumentNullException("clientId"))
        if String.IsNullOrWhiteSpace(clientSecret) then
            raise (ArgumentNullException("clientSecret"))
    
    let optionChainCache = OptionChainCache.OptionChainCache()
    
    
    let _httpClient = new HttpClient()
    let _asyncLock = AsyncLock()

    let isStockType assetType =
        match assetType with
        | "EQUITY" -> true
        | "ETF" -> true
        | "COLLECTIVE_INVESTMENT" -> true
        | _ -> false
    
    static let _retryPolicy =
        Policy
            .HandleInner<RateLimitRejectedException>()
            .WaitAndRetryAsync(
                retryCount = 3,
                sleepDurationProvider = (fun retryAttempt -> TimeSpan.FromSeconds(Math.Pow(2.0, float retryAttempt)))
            )

    static let _rateLimit =
        Policy.RateLimitAsync(
            numberOfExecutions = 20,
            perTimeSpan = TimeSpan.FromSeconds(1.0),
            maxBurst = 10
        )

    static let _timeoutPolicy =
        Policy.TimeoutAsync(
            seconds = int (TimeSpan.FromSeconds(255.0).TotalSeconds)
        )

    let _wrappedPolicy =
        Policy.WrapAsync(
            _rateLimit, _timeoutPolicy, _retryPolicy
        )
        
    let generateUrl (url: SchwabResourceEndpoint) =
        match url with
        | AuthAUrl path -> $"https://api.schwabapi.com/v1{path}"
        | TraderApiUrl path -> $"https://api.schwabapi.com/trader/v1{path}"
        | MarketDataUrl path -> $"https://api.schwabapi.com/marketdata/v1{path}"
    
    let logInfo message ([<ParamArray>]args: obj array) =
        match logger with
        | Some logger -> logger.LogInformation(message, args)
        | None -> ()
    
    let logError message ([<ParamArray>]args: obj array) =
        match logger with
        | Some logger -> logger.LogError(message, args)
        | None -> ()
    
    let logIfFailed (response: HttpResponseMessage) message = task {
        if not response.IsSuccessStatusCode then
            let! content = response.Content.ReadAsStringAsync()
            logError "Schwab client failed with {statusCode} for {message}: {content}" [|response.StatusCode; message; content|]
    }
    
    let notConnectedToBrokerageError = "User is not connected to brokerage" |> ServiceError
    
    let execIfConnectedToBrokerage (state:UserState) connectedStateFunc = task {
        if not state.ConnectedToBrokerage then
            return notConnectedToBrokerageError |> Error
        else
            return! connectedStateFunc state
    }
    
    // I am most definitely not handling this right, but essentially schwab returns adjusted prices
    // and my position price tracking is based on non-adjusted prices, so when I do historical simulations
    // it's all out of whack. I need to figure out how to handle this properly
    // what I am doing right now is returning an adjustment that changes values if date condition is met
    // I don't even know where I would get these adjustments. For futu for example, this is not
    // a stock split, I just see $1.95 divident bubble on trading view and sure enough, before that date
    // all the prices are off by 1.95 when comparing to adjusted vs unadjusted. Still not sure what is happening here
    // but all the brokerages etc. are handling this
    let getAdjustments ticker =
        match ticker with
        | "FUTU" ->
            let thresholdDate = DateTimeOffset.Parse("2024-12-06")
            (fun (dt:DateTimeOffset) (value:decimal) ->
                match dt.Date <= thresholdDate.Date with
                | true -> value + 1.95m
                | false -> value
            )
        | _ -> (fun _ value -> value)

    let safeParseDate (str:string option) = 
        match str with
        | None -> None
        | Some str ->
            match DateTimeOffset.TryParse str with
            | true, dt -> Some dt
            | _ -> None
    
    let safeParseDateString (str:string) = 
        safeParseDate (Some str)
    
    let refreshAccessTokenInternal (user: UserState) (fullRefresh: bool) = task {
        let postData =
            dict [
                "grant_type", "refresh_token"
                "refresh_token", user.BrokerageRefreshToken
                "client_id", clientId
                if fullRefresh then "access_type", "offline"
            ]
            
        let url = "/oauth/token" |> AuthAUrl |> generateUrl

        use request = new HttpRequestMessage(HttpMethod.Post, url)
        request.Headers.Authorization <- AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")))
        request.Content <- new FormUrlEncodedContent(postData)
        
        use! response = _httpClient.SendAsync(request)

        do! logIfFailed response "refresh access token"

        let! responseString = response.Content.ReadAsStringAsync()

        let deserialized = JsonSerializer.Deserialize<OAuthResponse>(responseString)

        return deserialized
    }
    
    let getBuyOrderType(brokerageOrderType: BrokerageOrderType) =
        match brokerageOrderType with
        | Limit -> "LIMIT"
        | Market -> "MARKET"
        | StopMarket -> "STOP"
        
    let getPrice (brokerageOrderType: BrokerageOrderType) (price: decimal) =
        match brokerageOrderType with
        | Limit -> Some price
        | Market -> None
        | StopMarket -> None

    let getActivationPrice (brokerageOrderType: BrokerageOrderType) (price: decimal) =
        match brokerageOrderType with
        | Limit -> None
        | Market -> None
        | StopMarket -> Some price

    let getBuyOrderDuration(brokerageOrderDuration: BrokerageOrderDuration) =
        match brokerageOrderDuration with
        | Day -> "DAY"
        | GTC -> "GOOD_TILL_CANCEL"
        | DayPlus -> "DAY"
        | GtcPlus -> "GOOD_TILL_CANCEL"

    let getSession(brokerageOrderDuration: BrokerageOrderDuration) =
        match brokerageOrderDuration with
        | Day -> "NORMAL"
        | GTC -> "NORMAL"
        | DayPlus -> "SEAMLESS"
        | GtcPlus -> "SEAMLESS"
        
    let parseOrderStatus status =
        match status with
        | "WORKING" -> OrderStatus.Working
        | "FILLED" -> OrderStatus.Filled
        | "CANCELED" -> OrderStatus.Canceled
        | "REJECTED" -> OrderStatus.Rejected
        | "EXPIRED" -> OrderStatus.Expired
        | "PENDING_ACTIVATION" -> OrderStatus.PendingActivation
        | "ACCEPTED" -> OrderStatus.Accepted
        | "REPLACED" -> OrderStatus.Replaced
        | _ -> failwith $"Unknown order status: {status}"
        
    let parseStockOrderType orderType =
        match orderType with
        | "LIMIT" -> StockOrderType.Limit
        | "MARKET" -> StockOrderType.Market
        | "STOP" -> StockOrderType.StopMarket
        | _ -> failwith $"Unknown stock order type: {orderType}"
    
    let parseOptionOrderType orderType =
        match orderType with
        | "LIMIT" -> OptionOrderType.Limit
        | "MARKET" -> OptionOrderType.Market
        | "NET_DEBIT" -> OptionOrderType.NetDebit
        | "NET_CREDIT" -> OptionOrderType.NetCredit
        | _ -> failwith $"Unknown option order type: {orderType}"
        
        
    let parseStockOrderInstruction instruction =
        match instruction with
        | "BUY" -> StockOrderInstruction.Buy
        | "SELL" -> StockOrderInstruction.Sell
        | "SELL_SHORT" -> StockOrderInstruction.SellShort
        | "BUY_TO_COVER" -> StockOrderInstruction.BuyToCover
        | _ -> failwith $"Unknown stock order instruction: {instruction}"
        
    let parseOptionOrderInstruction instruction =
        match instruction with
        | "BUY_TO_OPEN" -> OptionOrderInstruction.BuyToOpen
        | "BUY_TO_CLOSE" -> OptionOrderInstruction.BuyToClose
        | "SELL_TO_OPEN" -> OptionOrderInstruction.SellToOpen
        | "SELL_TO_CLOSE" -> OptionOrderInstruction.SellToClose
        | _ -> failwith $"Unknown option order instruction: {instruction}"
        
        
    let parseAssetType assetType =
        match assetType with
        | "EQUITY" -> AssetType.Equity
        | "OPTION" -> AssetType.Option
        | "ETF" -> AssetType.ETF
        | "COLLECTIVE_INVESTMENT" -> AssetType.ETF
        | _ -> failwith $"Unknown asset type: {assetType}"
        
    let mapSchwabQuoteResponseToStockResponse symbol (schwabResponse:SchwabStockQuoteResponse) : StockQuote =
        {
            symbol = symbol |> Ticker
            bidPrice = schwabResponse.quote.bidPrice
            exchange = schwabResponse.quote.exchange
            mark =  schwabResponse.quote.mark
            volatility = schwabResponse.quote.volatility
            askPrice = schwabResponse.quote.askPrice
            askSize = schwabResponse.quote.askSize
            bidSize = schwabResponse.quote.bidSize
            closePrice = schwabResponse.quote.closePrice
            exchangeName = schwabResponse.quote.exchangeName
            lastPrice = schwabResponse.quote.lastPrice
            lastSize = schwabResponse.quote.lastSize
            regularMarketLastPrice = schwabResponse.regular.regularMarketLastPrice
        }
        
    let mapStockOrder (o: OrderStrategy, l: OrderLeg) =
        {
            Price = o.ResolveStockPrice()
            Quantity = l.quantity
            Status = o.status |> parseOrderStatus
            StatusDescription = o.statusDescription
            Type = o.orderType |> parseStockOrderType
            Instruction = l.instruction |> parseStockOrderInstruction
            Ticker = Ticker l.instrument.symbol
            ExecutionTime = o.closeTime |> safeParseDate
            EnteredTime = o.enteredTime |> safeParseDateString
            ExpirationTime = o.cancelTime |> safeParseDate
            OrderId = o.orderId.ToString()
            CanBeCancelled = o.cancelable
        }
        
    let mapOptionOrder (o:OrderStrategy) : OptionOrder =
        {
            Price = o.ResolveOptionPrice()
            Quantity = o.quantity |> decimal
            Status = o.status |> parseOrderStatus
            Type = o.orderType |> parseOptionOrderType
            EnteredTime = o.enteredTime |> safeParseDateString
            ExecutionTime = o.closeTime |> safeParseDate
            ExpirationTime = o.cancelTime |> safeParseDate
            OrderId = o.orderId.ToString()
            CanBeCancelled = o.cancelable
            Contracts = o.orderLegCollection.Value |> Array.map(fun l ->
                
                // description is in the following format:
                // FLOOR & DECOR HLDGS INC 11/15/2024 $115 Put
                // so we need to extract the expiration date and the strike price using regex
                let rm = System.Text.RegularExpressions.Regex.Match(l.instrument.description, @"(\d{1,2}/\d{1,2}/\d{4}) \$?(\d{1,3}\.?\d{0,2}) (Put|Call)")
                
                let expirationString = rm.Groups[1].Value

                match DateTimeOffset.TryParseExact(expirationString, "MM/dd/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None) with
                | false, _ -> failwith $"Could not parse option expiration date from string: {expirationString} for {l.instrument.description}"
                | true, _ -> ()

                let expiration = DateTimeOffset.ParseExact(expirationString, "MM/dd/yyyy", CultureInfo.InvariantCulture) |> OptionExpiration.createFromDateTimeOffset
                let strikePrice = rm.Groups[2].Value |> decimal
                let instruction = l.instruction |> parseOptionOrderInstruction
                let quantityFactor =
                    match instruction with
                    | OptionOrderInstruction.BuyToOpen -> 1
                    | OptionOrderInstruction.BuyToClose -> 1
                    | OptionOrderInstruction.SellToOpen -> -1
                    | OptionOrderInstruction.SellToClose -> -1
                
                {
                    LegId = l.legId.ToString()
                    Cusip = l.cusip
                    Description = l.instrument.description
                    OptionType = if l.instrument.putCall = "PUT" then OptionType.Put else OptionType.Call  
                    UnderlyingTicker = l.instrument.underlyingSymbol |> Ticker 
                    Ticker = l.instrument.symbol |> OptionTicker
                    Quantity = (l.quantity |> int) * quantityFactor
                    Instruction = instruction
                    Price = o.ResolveOptionLegPrice(l.legId)
                    Expiration = expiration
                    StrikePrice = strikePrice 
                }
            ) 
        }
        
    let getAccessToken (user:UserState) = task {
        if not user.ConnectedToBrokerage then
            return raise (Exception("User is not connected to brokerage"))
        else
            // go to the storage to check for access token there
            let storageKey = "access-token-schwab:" + user.Id.ToString()
            let! firstCheck = blobStorage.Get<OAuthResponse>(storageKey)
            
            match firstCheck with
            | Some t when t.IsExpired = false -> return t
            | _ ->
                
                // TODO: this looks suspect
                let! lock = _asyncLock.LockAsync()
                use _ = lock
                
                // check again, in case another thread has already refreshed the token
                let! secondCheck = blobStorage.Get<OAuthResponse>(storageKey)
                match secondCheck with
                | Some t when t.IsExpired = false -> return t
                | _ ->
                    logInfo "Refreshing access token" [||]
                    
                    let! t = refreshAccessTokenInternal user false
                    t.created <- Some DateTimeOffset.UtcNow

                    match t.IsError with
                    | true ->
                        logError "Could not refresh access token: {error}" [|t.error|]
                        return raise (Exception("Could not refresh access token: " + t.error))
                    | false ->
                        logInfo "Saving access token to storage" [||]
                    
                        do! blobStorage.Save(storageKey, t)
                        return t
    }
    
    member private this.CallApiWithoutSerialization(user: UserState) (resource: SchwabResourceEndpoint) (method: HttpMethod) (jsonData: string option) = task {
        let! oauth = getAccessToken user
        
        let makeCallAndGetResponse (ct:CancellationToken) = task {
            use request = new HttpRequestMessage(method, resource |> generateUrl)
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", oauth.access_token)
            if jsonData.IsSome then
                request.Content <- new StringContent(jsonData.Value, Encoding.UTF8, "application/json")
            use! response = _httpClient.SendAsync(request, ct)
            let! content = response.Content.ReadAsStringAsync(ct)
            return response, content
        }
        
        let action = Func<CancellationToken, Task<HttpResponseMessage * string>> makeCallAndGetResponse
        
        let! tuple = _wrappedPolicy.ExecuteAsync(action, CancellationToken.None)

        return tuple
    }
    
    member private this.CallApi<'T> (user: UserState) (resource: SchwabResourceEndpoint) (debug: bool option) = task {
        try
            let sw = System.Diagnostics.Stopwatch.StartNew()
            let! response, content = this.CallApiWithoutSerialization user resource HttpMethod.Get None
            sw.Stop()
            
            match debug with
            | Some true ->
                logInfo "Call to {function} took {time}ms" [|resource; sw.ElapsedMilliseconds|]
            | _ -> ()
                
            if not response.IsSuccessStatusCode then
                try
                    let error = JsonSerializer.Deserialize<ErrorResponse>(content)
                    match error.message with
                    | Some error -> return error |> ServiceError |> Error
                    | None -> return content |> ServiceError |> Error
                with
                | :? JsonException ->
                    logError "Failed to deserialize error response from {function}: {content}" [|resource; content|]
                    return content |> ServiceError |> Error
            else
                let deserialized = JsonSerializer.Deserialize<'T>(content)
                return deserialized |> Ok

        with
        | :? TimeoutRejectedException as e ->
            logError "Timeout function {function} with exception: {exception}" [|resource; e.Message|]
            return e.Message |> ServiceError |> Error
        | e ->
            logError "Failed to call {function} with exception: {exception}" [|resource; e|]
            return e.Message |> ServiceError |> Error
    }
    
    member private this.GetQuote (tickers:Ticker seq) (state:UserState) = task {
        let commaSeparated = String.concat "," (tickers |> Seq.map _.Value)
        let resource = $"/quotes?symbols={commaSeparated}" |> MarketDataUrl

        let! result = this.CallApi<Dictionary<string, SchwabStockQuoteResponse>> state resource None

        // doing this conversion as I can't seem to find a way for system.text.json to support dictionary deserialization where
        // the key is something other than string. If I put Ticker in there, I get a runtime error. Tried to use converters
        // but that did not work, seems like you had to add dictionary converter and it got ugly pretty quickly
        return
            match result with
            | Error error -> Error error
            | Ok result ->
                Ok (result |> Seq.map (fun (KeyValue(k, v)) -> KeyValuePair(Ticker k, (v |> mapSchwabQuoteResponseToStockResponse k))) |> Dictionary)
    }
    
    member this.EnterOrder(user: UserState) (postData: obj) = task {
        let enterOrderExecution user = task {
            let! response = this.CallApi<AccountNumber []> user ("/accounts/accountNumbers" |> TraderApiUrl) None
            
            match response with
            | Error error -> return Error error
            | Ok accounts ->
                let accountId = accounts[0].hashValue

                let resource = $"/accounts/{accountId}/orders" |> TraderApiUrl

                let data =
                    match postData with
                    | :? string as data -> data
                    | _ -> JsonSerializer.Serialize(postData)

                let! enterResponse, content = this.CallApiWithoutSerialization user resource HttpMethod.Post (Some data)

                logInfo "Enter order response: {content}" [|content|]

                return
                    match enterResponse.IsSuccessStatusCode with
                    | true -> Ok ()
                    | false -> content |> ServiceError |> Error
        }
        
        return! execIfConnectedToBrokerage user enterOrderExecution
    }
    
    interface IBrokerage with 
    
        member this.GetAccessToken(user: UserState) = getAccessToken user
        
        member _.ConnectCallback(code: string) = task {
            logInfo "Starting ConnectCallback with code: {code}" [|code|]

            let postData =
                dict [
                    "grant_type", "authorization_code"
                    "code", code
                    "access_type", "offline"
                    "redirect_uri", callbackUrl
                    "client_id", clientId
                ]
            
            logInfo "Post data prepared: {postData}" [|postData|]
            
            use request = new HttpRequestMessage(HttpMethod.Post, "/oauth/token" |> AuthAUrl |> generateUrl)
            
            request.Content <- new FormUrlEncodedContent(postData)
            
            request.Headers.Authorization <- AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}")))

            logInfo "Request prepared: {request}" [|request|]
            logInfo "Request URI: {requestUri}" [|request.RequestUri|]
            logInfo "Request Headers:" [||]
            for header in request.Headers do
                match header.Key with
                | "Authorization" -> logInfo "  {key}: {value}" [|header.Key; "********"|]
                | _ -> logInfo "  {key}: {value}" [|header.Key; header.Value|]

            logInfo "Sending request..." [||]
            use! response = _httpClient.SendAsync(request)
            
            logInfo "Response received. Status code: {statusCode}" [|response.StatusCode|]

            match response.IsSuccessStatusCode with
            | false ->
                logError "Request failed with status code: {statusCode}" [|response.StatusCode|]
                do! logIfFailed response "connect callback"
                return ServiceError "Failed to connect to brokerage" |> Error
            | true ->
                logInfo "Request successful" [||]
                let! responseString = response.Content.ReadAsStringAsync()
                
                try
                    let result = JsonSerializer.Deserialize<OAuthResponse>(responseString)
                    return Ok result
                with
                | ex ->
                    logError "Failed to deserialize response: {message}" [|ex.Message|]
                    return ServiceError "Failed to parse brokerage response" |> Error
        }
        member _.GetOAuthUrl() = task {
            let encodedClientId = Uri.EscapeDataString(clientId)
            let encodedCallbackUrl = Uri.EscapeDataString(callbackUrl)
            return $"/oauth/authorize?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}" |> AuthAUrl |> generateUrl
        }

        member this.GetStockProfile (state: UserState) (ticker: Ticker) = task {
            let execution state = task {
                let resource = $"/instruments?symbol={ticker.Value}&projection=fundamental" |> MarketDataUrl
                
                let! results = this.CallApi<InstrumentsResponse> state resource None
                
                return
                    results
                    |> Result.map(fun results ->
                        match results.instruments with
                        | Some instruments -> instruments
                        | None -> [||])
                    |> Result.map(fun instruments ->
                        let fundamentals =
                            match instruments[0].fundamental with
                            | Some f ->
                                let keyValuePairs =
                                    f
                                    |> Seq.map (fun kvp -> KeyValuePair<string,string>(kvp.Key, kvp.Value.ToString()))
                                Dictionary<string, string>(keyValuePairs)
                            | None -> Dictionary<string, string>()
                        let data = instruments[0]

                        let mapped : StockProfile = {
                            Symbol = data.symbol
                            Description = data.description
                            SecurityName = data.description
                            Exchange = data.exchange
                            Cusip = data.cusip
                            IssueType = data.assetType
                            Fundamentals = fundamentals
                        }
                        
                        mapped
                    )
            }
            
            return! execIfConnectedToBrokerage state execution
        }

        member this.Search (state: UserState) (searchType: SearchQueryType) (query: string) (limit: int) = task {
            
            let connectedStateFunc state = task {
                // schwab API freaks out if the query is only a single letter, doesn't return an error and instead
                // returns a body that has a single character that is not a valid json, so we need to check for that
                if query.Length <= 1 then
                    return Ok [||]
                else
                    
                    let projection =
                        match searchType with
                        | Symbol -> "symbol-regex"
                        | Description -> "desc-regex"
                    
                    let resource = $"/instruments?symbol={query}.*&projection={projection}" |> MarketDataUrl
                    
                    let! results = this.CallApi<InstrumentsResponse> state resource None
                    
                    return
                        results
                        |> Result.map(fun results -> results.instruments |> Option.defaultValue [||])
                        |> Result.map(fun instruments ->
                            instruments
                            |> Seq.filter (fun i -> i.assetType |> isStockType)
                            |> Seq.map (fun i ->
                                {
                                    Symbol = i.symbol |> Ticker
                                    SecurityName = i.description
                                    Exchange = i.exchange
                                    AssetType = i.assetType |> parseAssetType
                                }
                            )
                            |> Seq.sortBy (fun r ->
                                match r.Exchange with
                                | "Pink Sheet" -> 1
                                | _ -> 0
                            )
                            |> Seq.sortBy (fun r ->
                                match r.AssetType with
                                | Equity -> 0
                                | Option -> 1
                                | ETF -> 2
                                // | "MUTUAL_FUND" -> 2
                                // | "ETF" -> 3
                                // | "INDEX" -> 4
                                // | "CASH_EQUIVALENT" -> 5
                                // | "FIXED_INCOME" -> 6
                                // | "CURRENCY" -> 7
                                // | _ -> 8
                            )
                            |> Seq.truncate limit
                            |> Seq.toArray
                        )
            }
            
            return! execIfConnectedToBrokerage state connectedStateFunc
        }
        
        member this.GetTransactions(user: UserState) (types : AccountTransactionType array) = task {
            
            // from the docs ...
            // TRADE, RECEIVE_AND_DELIVER, DIVIDEND_OR_INTEREST, ACH_RECEIPT, ACH_DISBURSEMENT, CASH_RECEIPT, CASH_DISBURSEMENT,
            // ELECTRONIC_FUND, WIRE_OUT, WIRE_IN, JOURNAL, MEMORANDUM, MARGIN_CALL, MONEY_MARKET, SMA_ADJUSTMENT    
            let mapTransactionTypeToString transactionType =
                match transactionType with
                | Dividend -> "DIVIDEND_OR_INTEREST"
                | Fee -> "DIVIDEND_OR_INTEREST"
                | Interest -> "DIVIDEND_OR_INTEREST"
                | Trade -> "TRADE"
                | Transfer -> "ACH_RECEIPT,ACH_DISBURSEMENT,CASH_RECEIPT,CASH_DISBURSEMENT,ELECTRONIC_FUND,WIRE_OUT,WIRE_IN"
                | _ -> failwith "Unsupported transaction type " + transactionType.ToString()
            
            let accountFunc state = task {
                
                let accountNumbersResource = "/accounts/accountNumbers" |> TraderApiUrl
                let! accountNumbers = this.CallApi<AccountNumber []> state accountNumbersResource None
                match accountNumbers with
                | Error error -> return Error error
                | Ok accountNumbers ->
                    let accountId = accountNumbers[0].hashValue
                    
                    // ISO-8601 format
                    let format = "yyyy-MM-dd'T'HH:mm:ss.000Z"
                    let startDate = DateTimeOffset.UtcNow.AddMonths(-11).ToString(format)
                    let endDate = DateTimeOffset.UtcNow.AddDays(1).ToString(format)
                    let types = types |> Array.map mapTransactionTypeToString |> Array.distinct |> String.concat(",")
                    
                    let ordersResource = $"/accounts/{accountId}/transactions?startDate={startDate}&endDate={endDate}&types={types}" |> TraderApiUrl
                    let! transactions = this.CallApi<TransactionItem []> state ordersResource None
                    match transactions with
                    | Error error -> return Error error
                    | Ok transactions ->
                        
                        let accountTransactions =
                            transactions
                            |> Array.map(fun t ->
                                
                                let accountTransaction = {
                                    TransactionId = t.activityId.ToString()
                                    Description = t.description
                                    BrokerageType = t.``type``
                                    NetAmount = t.netAmount
                                    SettlementDate = t.settlementDate |> DateTimeOffset.Parse
                                    TradeDate = t.tradeDate |> DateTimeOffset.Parse
                                    InferredTicker = None
                                    InferredType = None
                                    Inserted = None
                                    Applied = None 
                                }
                                accountTransaction
                            )
                            |> Array.sortBy _.TradeDate

                        return accountTransactions |> Ok
            }
            
            return! execIfConnectedToBrokerage user accountFunc    
        }

        member this.GetAccount(user: UserState) = task {
            
            let accountFunc state = task {
                
                let accountNumbersResource = "/accounts/accountNumbers" |> TraderApiUrl
                let! accountNumbers = this.CallApi<AccountNumber []> state accountNumbersResource None
                match accountNumbers with
                | Error error -> return Error error
                | Ok accountNumbers ->
                    let accountId = accountNumbers[0].hashValue
                    
                    // ISO-8601 format
                    let format = "yyyy-MM-dd'T'HH:mm:ss.000Z"
                    let fromEnteredTime = DateTimeOffset.UtcNow.AddMonths(-6).ToString(format)
                    let toEnteredTime = DateTimeOffset.UtcNow.AddDays(1).ToString(format)
                    
                    let ordersResource = $"/accounts/{accountId}/orders?fromEnteredTime={fromEnteredTime}&toEnteredTime={toEnteredTime}" |> TraderApiUrl
                    let! orders = this.CallApi<OrderStrategy []> state ordersResource None
                    match orders with
                    | Error error -> return Error error
                    | Ok orders ->
                        
                        let brokerageStockOrders =
                            orders
                            |> Array.filter (fun o -> o.orderLegCollection |> Option.defaultValue [||] |> Array.exists(fun l -> l.instrument.assetType |> isStockType))
                            |> Array.collect(fun o -> o.orderLegCollection |> Option.defaultValue [||] |> Array.map(fun l -> o, l))
                            |> Array.map(mapStockOrder)
                            
                        let brokergeOptionOrders =
                            orders
                            |> Array.filter (fun o -> o.orderLegCollection |> Option.defaultValue [||] |> Array.exists(fun l -> l.instrument.assetType |> isStockType |> not))
                            |> Array.map(mapOptionOrder)

                        let resource = $"/accounts/{accountId}?fields=positions" |> TraderApiUrl
                        let! response = this.CallApi<AccountsResponse> state resource None
                        
                        return
                            response
                            |> Result.map(fun accounts ->
                                let tdPositions = if accounts.securitiesAccount.IsSome then accounts.securitiesAccount.Value.positions |> Option.defaultValue [||] else [||]
                                
                                let stockPositions =
                                    tdPositions
                                    |> Array.filter (fun p -> p.instrument.Value.assetType |> isStockType)
                                    |> Array.map (fun p ->
                                        let qty = if p.longQuantity > 0m then p.longQuantity else -p.shortQuantity
                                        BrokerageStockPosition(Ticker p.instrument.Value.symbol, p.averagePrice, qty)
                                    )

                                let optionPositions =
                                    tdPositions
                                    |> Array.filter (fun p -> p.instrument.Value.assetType = "OPTION")
                                    |> Array.map (fun p ->
                                        let description = p.instrument.Value.description
                                        // description looks like this: Marriott Intl Inc 09/20/2024 $225 Put
                                        // split it by space and get the parts from the end
                                        let split = description.Split(" ")
                                        
                                        match split with
                                        | x when x.Length > 3 ->
                                            let optionType = split[split.Length - 1].ToUpperInvariant()
                                            let strike = Decimal.Parse(split[split.Length - 2].TrimStart('$'))
                                            
                                            // expiration date right now comes through as 09/20/2024, generically as MM/dd/yyyy
                                            // I want to convert it to yyyy-MM-dd
                                            let expirationDate = split[split.Length - 3]
                                            let dateParts = expirationDate.Split("/")
                                            let day, month, year = dateParts[1], dateParts[0], dateParts[2]
                                            // validate that the date is correct
                                            let expirationDateAsString = $"{year}-{month}-{day}"
                                            // make sure we can parse with exact format
                                            match DateTimeOffset.TryParseExact(expirationDateAsString, "yyyy-MM-dd", null, DateTimeStyles.None) with
                                            | true, _ -> ()
                                            | false, _ -> failwith $"Could not parse expiration date: {expirationDateAsString}"
                                            
                                            let ticker = p.instrument.Value.underlyingSymbol
                                            
                                            let optionPosition = {
                                                BrokerageOptionPosition.Ticker = Ticker ticker
                                                Quantity = if p.longQuantity > 0m then int p.longQuantity else int -p.shortQuantity
                                                AverageCost = p.averagePrice
                                                StrikePrice = strike
                                                ExpirationDate = expirationDateAsString
                                                MarketValue = p.marketValue
                                                OptionType = optionType.ToUpperInvariant()
                                            }
                                            optionPosition
                                        | _ -> failwith $"Could not parse option description: {description}"
                                    )

                                let account = BrokerageAccount()
                                account.StockOrders <- brokerageStockOrders
                                account.StockPositions <- stockPositions
                                account.OptionPositions <- optionPositions
                                account.OptionOrders <- brokergeOptionOrders
                                account.CashBalance <- Some accounts.securitiesAccount.Value.currentBalances.Value.cashBalance
                                account.Equity <- Some accounts.securitiesAccount.Value.currentBalances.Value.equity
                                account.LongMarketValue <- Some accounts.securitiesAccount.Value.currentBalances.Value.longMarketValue
                                account.ShortMarketValue <- Some accounts.securitiesAccount.Value.currentBalances.Value.shortMarketValue
                                account
                            )
            }
            
            return! execIfConnectedToBrokerage user accountFunc    
        }

        member this.CancelOrder (user: UserState) (orderId: string) = task {
            let cancelExecution user = task {
                let resource = "/accounts/accountNumbers" |> TraderApiUrl
                let! response = this.CallApi<AccountNumber []> user resource None
                match response with
                | Error error -> return Error error
                | Ok accounts ->
                    let accountId = accounts[0].hashValue

                    let orderResource = $"/accounts/{accountId}/orders/{orderId}" |> TraderApiUrl

                    let! cancelResponse, content = this.CallApiWithoutSerialization user orderResource HttpMethod.Delete None

                    return
                        match cancelResponse.IsSuccessStatusCode with
                        | true -> Ok ()
                        | false -> content |> ServiceError |> Error
            }
            
            return! execIfConnectedToBrokerage user cancelExecution    
        }

        member this.BuyOrder (user: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (brokerageOrderType: BrokerageOrderType) (brokerageOrderDuration: BrokerageOrderDuration) =
            let legCollection =
                {|
                    instruction = "BUY"
                    quantity = numberOfShares
                    instrument = {| symbol = ticker.Value; assetType = "EQUITY" |}
                |}

            let postData =
                {|
                    orderType = getBuyOrderType brokerageOrderType
                    session = getSession brokerageOrderDuration
                    duration = getBuyOrderDuration brokerageOrderDuration
                    price = getPrice brokerageOrderType price
                    stopPrice = getActivationPrice brokerageOrderType price
                    orderStrategyType = "SINGLE"
                    orderLegCollection = [| legCollection |]
                |}

            this.EnterOrder user postData

        member this.BuyToCoverOrder (user: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (brokerageOrderType: BrokerageOrderType) (brokerageOrderDuration: BrokerageOrderDuration) =
            let legCollection =
                {|
                    instruction = "BUY_TO_COVER"
                    quantity = numberOfShares
                    instrument = {| symbol = ticker.Value; assetType = "EQUITY" |}
                |}

            let postData =
                {|
                    orderType = getBuyOrderType brokerageOrderType
                    session = getSession brokerageOrderDuration
                    duration = getBuyOrderDuration brokerageOrderDuration
                    price = getPrice brokerageOrderType price
                    stopPrice = getActivationPrice brokerageOrderType price
                    orderStrategyType = "SINGLE"
                    orderLegCollection = [| legCollection |]
                |}

            this.EnterOrder user postData

        member this.SellShortOrder (user: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (brokerageOrderType: BrokerageOrderType) (brokerageOrderDuration: BrokerageOrderDuration) =
            let legCollection =
                {|
                    instruction = "SELL_SHORT"
                    quantity = numberOfShares
                    instrument = {| symbol = ticker.Value; assetType = "EQUITY" |}
                |}

            let postData =
                {|
                    orderType = getBuyOrderType brokerageOrderType
                    session = getSession brokerageOrderDuration
                    duration = getBuyOrderDuration brokerageOrderDuration
                    price = getPrice brokerageOrderType price
                    stopPrice = getActivationPrice brokerageOrderType price
                    orderStrategyType = "SINGLE"
                    orderLegCollection = [| legCollection |]
                |}

            this.EnterOrder user postData

        member this.SellOrder (user: UserState) (ticker: Ticker) (numberOfShares: decimal) (price: decimal) (brokerageOrderType: BrokerageOrderType) (brokerageOrderDuration: BrokerageOrderDuration) =
            let legCollection =
                {|
                    instruction = "SELL"
                    quantity = numberOfShares
                    instrument = {| symbol = ticker.Value; assetType = "EQUITY" |}
                |}

            let postData =
                {|
                    orderType = getBuyOrderType brokerageOrderType
                    session = getSession brokerageOrderDuration
                    duration = getBuyOrderDuration brokerageOrderDuration
                    price = getPrice brokerageOrderType price
                    orderStrategyType = "SINGLE"
                    orderLegCollection = [| legCollection |]
                |}

            this.EnterOrder user postData
            
        member this.OptionOrder (user: UserState) (payload:string) = 
            this.EnterOrder user payload
            
        member this.GetQuotes (user: UserState) (tickers: Ticker seq) = execIfConnectedToBrokerage user (this.GetQuote tickers)
        
        member this.GetQuote (user: UserState) (ticker: Ticker) = task {
            let! response = this.GetQuote [ticker] user
            
            return
                match response with
                | Error error -> Error error
                | Ok quotes ->
                    match quotes.TryGetValue ticker with
                    | true, quote -> Ok quote
                    | _ -> ServiceError "Could not find quote for ticker" |> Error
        }

        member this.GetOptionChain (state: UserState) (source:OptionChainSource) (ticker: Ticker) = task {
            
            let execFunc state = task {

                let cachedValue =
                    match optionChainCache.TryGetValue(ticker) with
                    | Some chain -> Some chain
                    | None -> None
                    
                match cachedValue.IsSome && source = OptionChainSource.UseCache with
                | true -> return Ok cachedValue.Value
                | false ->
                    let resource = $"/chains?symbol={ticker.Value}" |> MarketDataUrl
                    
                    let! chainResponse = this.CallApi<OptionChain> state resource None
                    
                    match chainResponse with
                    | Error error -> return Error error
                    | Ok chain ->
                        let toOptionDetails (map: Dictionary<string, Dictionary<string, OptionDescriptor[]>>) underlyingPrice =
                            map
                            |> Seq.collect (fun (KeyValue(_, v)) -> v.Values |> Seq.collect id)
                            |> Seq.map (fun d ->
                                let expirationDate =
                                    match DateTimeOffset.TryParse(d.expirationDate) with
                                    | false, _ -> failwith $"Could not parse expiration date: {d.expirationDate}"
                                    | true, dt -> dt |> OptionExpiration.createFromDateTimeOffset
                                    
                                let optionType =
                                    match d.putCall.Value.ToLower() with
                                    | "put" -> OptionType.Put
                                    | "call" -> OptionType.Call
                                    | _ -> failwith $"Unknown option type: {d.putCall.Value}"
                                
                                let detail = core.fs.Adapters.Options.OptionDetail(d.symbol.Value, optionType, d.description.Value, expirationDate)
                                detail.Ask <- d.ask
                                detail.Bid <- d.bid
                                detail.Last <- d.last
                                detail.Mark <- d.mark
                                detail.StrikePrice <- d.strikePrice
                                detail.Volume <- d.totalVolume
                                detail.OpenInterest <- d.openInterest
                                detail.DaysToExpiration <- d.daysToExpiration
                                detail.Delta <- d.delta
                                detail.Gamma <- d.gamma
                                detail.Theta <- d.theta
                                detail.Vega <- d.vega
                                detail.Rho <- d.rho
                                detail.ExchangeName <- d.exchangeName
                                detail.InTheMoney <- d.inTheMoney
                                detail.IntrinsicValue <- d.intrinsicValue
                                detail.TimeValue <- d.timeValue
                                detail.Volatility <- d.volatility
                                detail.MarkChange <- d.markChange
                                detail.MarkPercentChange <- d.markPercentChange
                                detail.UnderlyingPrice <- underlyingPrice
                                detail
                            )

                        let options =
                            Seq.concat [
                                toOptionDetails chain.callExpDateMap chain.underlyingPrice
                                toOptionDetails chain.putExpDateMap chain.underlyingPrice
                            ]
                            |> Seq.toArray
                            
                        let mapped = core.fs.Adapters.Options.OptionChain(chain.symbol.Value, chain.volatility, chain.numberOfContracts, options, chain.underlyingPrice)
                        
                        optionChainCache.Set(ticker, mapped)
                        
                        return Ok mapped
            }
            
            return! execIfConnectedToBrokerage state execFunc
        }

        member this.GetMarketHours(state: UserState) (date: DateTimeOffset) = task {
            let execFunc state = task {
                let dateStr = date.ToString("yyyy-MM-dd")
                let resource = $"/markets?date={dateStr}&markets=equity" |> MarketDataUrl

                let! wrapper = this.CallApi<MarketHoursWrapper> state resource None

                return
                    match wrapper with
                    | Error error -> Error error
                    | Ok wrapper ->
                        match wrapper.equity with
                        | None -> ServiceError "Could not find market hours for date" |> Error
                        | Some equity ->
                            match equity.EQ with
                            | None -> ServiceError "Could not find market hours for date (EQ)" |> Error
                            | Some hours -> hours |> Ok
            }
            
            return! execIfConnectedToBrokerage state execFunc
        }

        member this.GetPriceHistory (state: UserState) (ticker: Ticker) (frequency: PriceFrequency) (startDate: DateTimeOffset option) (endDate: DateTimeOffset option) = task {
            
            let execFunc state = task {
                let fromOption (option:DateTimeOffset option) (defaultValue:DateTimeOffset) =
                    match option with
                    | Some value -> value.ToUnixTimeMilliseconds()
                    | None -> defaultValue.ToUnixTimeMilliseconds()

                let startUnix = fromOption startDate (DateTimeOffset.UtcNow.AddYears(-2))
                let endUnix = fromOption endDate DateTimeOffset.UtcNow
                
                let frequencyType =
                    match frequency with
                    | PriceFrequency.Daily -> "daily"
                    | PriceFrequency.Weekly -> "weekly"
                    | PriceFrequency.Monthly -> "monthly"
                
                let resource = $"/pricehistory?periodType=month&frequencyType={frequencyType}&startDate={startUnix}&endDate={endUnix}&symbol={ticker.Value}" |> MarketDataUrl

                let! response = this.CallApi<PriceHistoryResponse> state resource None

                match response with
                | Error error -> return Error error
                | Ok prices ->
                    let candles = defaultArg prices.candles [||]
                    let adjustments = getAdjustments ticker.Value
                    let payload =
                        candles
                        |> Array.map (
                            fun c ->
                                let date = DateTimeOffset.FromUnixTimeMilliseconds(c.datetime)
                                PriceBar(
                                    date,
                                    c.``open`` |> adjustments date,
                                    c.high |> adjustments date,
                                    c.low |> adjustments date,
                                    c.close |> adjustments date,c.volume
                                )
                        )

                    if Array.isEmpty payload then
                        return $"No candles for historical prices for {ticker.Value}" |> ServiceError |> Error
                    else
                        return PriceBars(payload, frequency) |> Ok
            }
            
            return! execIfConnectedToBrokerage state execFunc
        }

        member this.RefreshAccessToken(user: UserState) = refreshAccessTokenInternal user true
