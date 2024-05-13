namespace SchwabClient

open System
open System.Collections.Generic
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

type ErrorResponse = {
    error: string option
}

type MarketHoursEquity = {
    EQ: MarketHours option
}

type MarketHoursWrapper = {
    equity: MarketHoursEquity option
}

type Candle = {
    datetime: int64
    open': decimal
    high: decimal
    low: decimal
    close: decimal
    volume: int
}

type PriceHistoryResponse = {
    candles: Candle [] option
}

type Instrument = {
    assetType: string option
    cusip: string option
    symbol: string option
    description: string option
    type': string option
    putCall: string option
    underlyingSymbol: string option
    resolvedSymbol: string option
}

type OrderLeg = {
    orderLegType: string option
    legId: int64
    instrument: Instrument option
    instruction: string option
    positionEffect: string option
    quantity: float
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

type OrderStrategy = {
    session: string option
    duration: string option
    orderType: string option
    cancelTime: string option
    complexOrderStrategyType: string option
    quantity: float
    filledQuantity: float
    remainingQuantity: float
    stopPrice: decimal option
    stopType: string option
    price: decimal option
    orderStrategyType: string option
    orderId: int64
    cancelable: bool
    editable: bool
    status: string option
    enteredTime: string option
    closeTime: string option
    tag: string option
    accountId: int64
    statusDescription: string option
    orderLegCollection: OrderLeg [] option
    orderActivityCollection: OrderActivity [] option
}
    with
    
        member this.IsPending = this.status = Some "WORKING"
        
        member this.ResolvePrice() =
            match this.orderActivityCollection with
            | Some activities ->
                let executionPrices =
                    activities
                    |> Array.choose (fun o ->
                        match o.executionLegs with
                        | Some legs -> Some(legs |> Array.map(fun l -> l.price))
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
    orderStrategies: OrderStrategy [] option
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

type SearchItem = {
    cusip: string option
    symbol: string option
    description: string option
    exchange: string option
    assetType: string option
}

type SearchItemWithFundamental = {
    cusip: string option
    symbol: string option
    description: string option
    exchange: string option
    assetType: string option
    fundamental: Dictionary<string, obj> option
}

type Fundamental = {
    symbol: string option
    high52: float
    low52: float
    dividendAmount: float
    dividendYield: float
    dividendDate: string option
    peRatio: float
    pegRatio: float
    pbRatio: float
    prRatio: float
    pcfRatio: float
    grossMarginTtm: float
    grossMarginMrq: float
    netProfitMarginTtm: float
    netProfitMarginMrq: float
    operatingMarginTtm: float
    operatingMarginMrq: float
    returnOnEquity: float
    returnOnAssets: float
    returnOnInvestment: float
    quickRatio: float
    currentRatio: float
    interestCoverage: float
    totalDebtToCapital: float
    ltDebtToEquity: float
    totalDebtToEquity: float
    epsTtm: float
    epsChangePercentTtm: float
    epsChangeYear: float
    epsChange: float
    revChangeYear: float
    revChangeTtm: float
    revChangeIn: float
    sharesOutstanding: float
    marketCapFloat: float
    marketCap: float
    bookValuePerShare: float
    shortIntToFloat: float
    shortIntDayToCover: float
    divGrowthRate3Year: float
    dividendPayAmount: float
    dividendPayDate: string option
    beta: float
    vol1DayAvg: float
    vol10DayAvg: float
    vol3MonthAvg: float
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
    expirationDate: int64
    ExpirationDate: DateTimeOffset
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

type SchwabClient(blobStorage: IBlobStorage, callbackUrl: string, clientId: string, clientSecret:string, logger: ILogger<SchwabClient> option) =
    
    do
        // make sure callback url, client id, and client secret are set
        if String.IsNullOrWhiteSpace(callbackUrl) then
            raise (ArgumentNullException("callbackUrl"))
        if String.IsNullOrWhiteSpace(clientId) then
            raise (ArgumentNullException("clientId"))
        if String.IsNullOrWhiteSpace(clientSecret) then
            raise (ArgumentNullException("clientSecret"))
    
    let _httpClient = new HttpClient()
    let _asyncLock = AsyncLock()

    let ApiUrl = "https://api.schwabapi.com/v1"

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
            seconds = int (TimeSpan.FromSeconds(15.0).TotalSeconds)
        )

    let _wrappedPolicy =
        Policy.WrapAsync(
            _rateLimit, _timeoutPolicy, _retryPolicy
        )
        
    let generateApiUrl(function': string) = $"{ApiUrl}/{function'}"
    
    let logDebug message ([<ParamArray>]args: obj array) =
        match logger with
        | Some logger -> logger.LogDebug(message, args)
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
    
    let refreshAccessTokenInternal (user: UserState) (fullRefresh: bool) = task {
        let postData =
            dict [
                "grant_type", "refresh_token"
                "refresh_token", user.BrokerageRefreshToken
                "client_id", clientId
            ]

        if fullRefresh then
            postData.Add("access_type", "offline")

        let content = new FormUrlEncodedContent(postData)
        
        let base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))
        
        content.Headers.Add("Authorization", $"Basic {base64Encoded}")

        let! response = _httpClient.PostAsync(generateApiUrl "/oauth/token", content)

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
        | Gtc -> "GOOD_TILL_CANCEL"
        | DayPlus -> "DAY"
        | GtcPlus -> "GOOD_TILL_CANCEL"

    let getSession(brokerageOrderDuration: BrokerageOrderDuration) =
        match brokerageOrderDuration with
        | Day -> "NORMAL"
        | Gtc -> "NORMAL"
        | DayPlus -> "SEAMLESS"
        | GtcPlus -> "SEAMLESS"
        
    let getAccessToken (user:UserState) = task {
        if not user.ConnectedToBrokerage then
            return raise (Exception("User is not connected to brokerage"))
        else
            // go to the storage to check for access token there
            let storageKey = "access-token-schwab:" + user.Id.ToString()
            let! token = blobStorage.Get<OAuthResponse>(storageKey)
            
            match token with
            | t when t.IsExpired = false -> return token
            | _ ->
                
                // TODO: this looks suspect
                let! lock = _asyncLock.LockAsync()
                use _ = lock
                
                // check again, in case another thread has already refreshed the token
                let! token = blobStorage.Get<OAuthResponse>(storageKey)
                match token with
                | t when t.IsExpired = false -> return token
                | _ ->
                    match logger with
                    | Some logger -> logger.LogInformation("Refreshing access token")
                    | None -> ()
                    
                    let! t = refreshAccessTokenInternal user false
                    t.created <- Some DateTimeOffset.UtcNow

                    match t.IsError with
                    | true ->
                        match logger with
                        | Some logger -> logger.LogError("Could not refresh access token: {error}", t.error)
                        | None -> ()
                        return raise (Exception("Could not refresh access token: " + t.error))
                    | false ->
                        match logger with
                        | Some logger -> logger.LogInformation("Saving access token to storage")
                        | None -> ()
                    
                        do! blobStorage.Save(storageKey, token)
                        return token
    }
    
    member private this.CallApiWithoutSerialization(user: UserState, function': string, method: HttpMethod, ?jsonData: string) = task {
        let! oauth = getAccessToken user

        let makeCallAndGetResponse (ct:CancellationToken) = task {
            let request = new HttpRequestMessage(method, generateApiUrl function')
            request.Headers.Authorization <- AuthenticationHeaderValue("Bearer", oauth.access_token)
            if jsonData.IsSome then
                request.Content <- new StringContent(jsonData.Value, Encoding.UTF8, "application/json")
            let! response = _httpClient.SendAsync(request, ct)
            let! content = response.Content.ReadAsStringAsync(ct)
            return response, content
        }
        
        let action = Func<CancellationToken, Task<HttpResponseMessage * string>> makeCallAndGetResponse
        
        let! tuple = _wrappedPolicy.ExecuteAsync(action, CancellationToken.None)

        return tuple
    }
    
    member private this.CallApi<'T>(user: UserState, function': string, method: HttpMethod, ?jsonData: string, ?debug: bool) = task {
        try
            let! response, content = this.CallApiWithoutSerialization(user, function', method, defaultArg jsonData null)

            if defaultArg debug false then
                logError "debug function: {function}" [|function'|]
                logError "debug response code: {statusCode}" [|response.StatusCode|]
                logError "debug response output: {content}" [|content|]
                
            if not response.IsSuccessStatusCode then
                let error = JsonSerializer.Deserialize<ErrorResponse>(content)
                match error.error with
                | Some error -> return error |> ServiceError |> Error
                | None -> return content |> ServiceError |> Error
            else
                let deserialized = JsonSerializer.Deserialize<'T>(content)
                return deserialized |> Ok

        with
        | :? TimeoutRejectedException as e ->
            logError "Timeout function {function} with exception: {exception}" [|function'; e.Message|]
            return e.Message |> ServiceError |> Error
    }
    
    member this.EnterOrder(user: UserState) (postData: obj) = task {
        let enterOrderExecution user = task {
            let! response = this.CallApi<AccountNumber []>(user, "/accounts/accountNumbers", HttpMethod.Get)
            
            match response with
            | Error error -> return Error error
            | Ok accounts ->
                let accountId = accounts[0].hashValue

                let url = $"/accounts/{accountId}/orders"

                let data = JsonSerializer.Serialize(postData)

                let! enterResponse, content = this.CallApiWithoutSerialization(user, url, HttpMethod.Post, data)

                return
                    if enterResponse.IsSuccessStatusCode then
                        Ok ()
                    else
                        content |> ServiceError |> Error
        }
        
        return! execIfConnectedToBrokerage user enterOrderExecution
    }
    
    interface IBrokerage with 
    
        member this.GetAccessToken(user: UserState) = getAccessToken user
        
        member _.ConnectCallback(code: string) = task {
            let postData =
                dict [
                    "grant_type", "authorization_code"
                    "code", code
                    "access_type", "offline"
                    "redirect_uri", callbackUrl
                    "client_id", clientId
                ]

            let content = new FormUrlEncodedContent(postData)
            
            let base64Encoded = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{clientId}:{clientSecret}"))
            
            content.Headers.Add("Authorization", $"Basic {base64Encoded}")

            let! response = _httpClient.PostAsync(generateApiUrl "/oauth/token", content)

            do! logIfFailed response "connect callback"

            let! responseString = response.Content.ReadAsStringAsync()

            logDebug "Response from schwab: {responseString}" [|responseString|]

            return JsonSerializer.Deserialize<OAuthResponse>(responseString)
        }

        member _.GetOAuthUrl() = task {
            let encodedClientId = Uri.EscapeDataString(clientId)
            let encodedCallbackUrl = Uri.EscapeDataString(callbackUrl)
            return $"{ApiUrl}/oauth/authorize?response_type=code&redirect_uri={encodedCallbackUrl}&client_id={encodedClientId}"
        }

        member this.GetAccount(user: UserState) = task {
            
            let accountFunc state = task {
                let! response = this.CallApi<AccountsResponse []>(
                    state,
                    "/accounts?fields=positions,orders",
                    HttpMethod.Get
                )

                return
                    response
                    |> Result.map(fun accounts ->
                        let tdPositions = if accounts[0].securitiesAccount.IsSome then accounts[0].securitiesAccount.Value.positions |> Option.defaultValue [||] else [||]
                        let strategies = if accounts[0].securitiesAccount.IsSome then accounts[0].securitiesAccount.Value.orderStrategies |> Option.defaultValue [||] else [||]

                        let orders =
                            strategies
                            |> Array.collect (fun o -> defaultArg o.orderLegCollection [||] |> Array.map (fun l -> o, l))
                            |> Array.map (fun (o, l) ->
                                let order = Order()
                                order.Date <- o.closeTime |> Option.map DateTimeOffset.Parse
                                order.Status <- o.status |> Option.defaultValue ""
                                order.OrderId <- o.orderId.ToString()
                                order.Quantity <- int l.quantity
                                order.Price <- o.ResolvePrice()
                                order.Cancelable <- o.cancelable
                                order.Ticker <- Ticker (l.instrument.Value.resolvedSymbol |> Option.defaultValue "") |> Some
                                order.Description <- l.instrument.Value.description |> Option.defaultValue ""
                                order.Type <- l.instruction |> Option.defaultValue ""
                                order.AssetType <- l.instrument.Value.assetType |> Option.defaultValue ""
                                order
                            )

                        let stockPositions =
                            tdPositions
                            |> Array.filter (fun p -> p.instrument.Value.assetType = Some "EQUITY")
                            |> Array.map (fun p ->
                                let qty = if p.longQuantity > 0m then p.longQuantity else -p.shortQuantity
                                StockPosition(Ticker p.instrument.Value.resolvedSymbol.Value, p.averagePrice, qty)
                            )

                        let optionPositions =
                            tdPositions
                            |> Array.filter (fun p -> p.instrument.Value.assetType = Some "OPTION")
                            |> Array.map (fun p ->
                                let description = p.instrument.Value.description.Value
                                // description looks like this: AGI Jul 21 2023 13.0 Call
                                // AGI is ticker, Jul 21 2023 is expiration date, 13.0 is strike price
                                // and Call is CALL type, parse all of these values from the description

                                match description.Split(" ") with
                                | [| ticker; expMonth; expDay; expYear; strike; optionType |] ->
                                    let optionPosition = OptionPosition()
                                    optionPosition.Ticker <- Some(Ticker ticker)
                                    optionPosition.Quantity <- if p.longQuantity > 0m then int p.longQuantity else int -p.shortQuantity
                                    optionPosition.AverageCost <- p.averagePrice
                                    optionPosition.StrikePrice <- decimal strike
                                    optionPosition.ExpirationDate <- $"{expMonth} {expDay} {expYear}"
                                    optionPosition.MarketValue <- p.marketValue
                                    optionPosition.OptionType <- optionType.ToUpperInvariant()
                                    optionPosition
                                | _ -> failwith $"Could not parse option description: {description}"
                            )

                        let account = BrokerageAccount()
                        account.Orders <- orders
                        account.StockPositions <- stockPositions
                        account.OptionPositions <- optionPositions
                        account.CashBalance <- Some accounts[0].securitiesAccount.Value.currentBalances.Value.cashBalance
                        account.Equity <- Some accounts[0].securitiesAccount.Value.currentBalances.Value.equity
                        account.LongMarketValue <- Some accounts[0].securitiesAccount.Value.currentBalances.Value.longMarketValue
                        account.ShortMarketValue <- Some accounts[0].securitiesAccount.Value.currentBalances.Value.shortMarketValue
                        account
                    )
            }
            
            return! execIfConnectedToBrokerage user accountFunc    
        }

        member this.CancelOrder (user: UserState) (orderId: string) = task {
            let cancelExecution user = task {
                // get account first
                let! response = this.CallApi<AccountNumber []>(user, "/accounts/accountNumbers", HttpMethod.Get)

                match response with
                | Error error -> return Error error
                | Ok accounts ->
                    let accountId = accounts[0].hashValue

                    let url = $"/accounts/{accountId}/orders/{orderId}"

                    let! cancelResponse, content = this.CallApiWithoutSerialization(user, url, HttpMethod.Delete)

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
                    instruction = "Buy"
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
                    instruction = "Sell"
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

        

        member this.RefreshAccessToken(user: UserState) = refreshAccessTokenInternal user true
        member this.GetMarketHours(state: UserState) (date: DateTimeOffset) = task {
            let execFunc state = task {
                let dateStr = date.ToString("yyyy-MM-dd")
                let function' = $"marketdata/EQUITY/hours?date={dateStr}"

                let! wrapper = this.CallApi<MarketHoursWrapper>(state, function', HttpMethod.Get)

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

    interface IStockInfoProvider with
    
        member this.GetStockProfile (state: UserState) (ticker: Ticker) = task {
            let execution state = task {
                let function' = $"instruments?symbol={ticker.Value}&projection=fundamental"
                
                let! results = this.CallApi<Dictionary<string, SearchItemWithFundamental>>(
                    state, function', HttpMethod.Get
                )
                
                return
                    results
                    |> Result.map(fun results ->
                        let fundamentals =
                            match results[ticker.Value].fundamental with
                            | Some f ->
                                let keyValuePairs =
                                    f
                                    |> Seq.map (fun kvp -> KeyValuePair<string,string>(kvp.Key, kvp.Value.ToString()))
                                Dictionary<string, string>(keyValuePairs)
                            | None -> Dictionary<string, string>()
                        let data = results[ticker.Value]

                        let mapped : StockProfile = {
                            Symbol = data.symbol |> Option.defaultValue ""
                            Description = data.description |> Option.defaultValue ""
                            SecurityName = data.description |> Option.defaultValue ""
                            Exchange = data.exchange |> Option.defaultValue ""
                            Cusip = data.cusip |> Option.defaultValue ""
                            IssueType = data.assetType |> Option.defaultValue ""
                            Fundamentals = fundamentals
                        }
                        
                        mapped
                    )
            }
            
            return! execIfConnectedToBrokerage state execution
        }

        member this.Search (state: UserState) (query: string) (limit: int) = task {
            
            let connectedStateFunc state = task {
                let function' = $"instruments?symbol={query}.*&projection=symbol-regex"

                let! results = this.CallApi<Dictionary<string, SearchItem>>(state, function', HttpMethod.Get)
                
                return
                    results
                    |> Result.map(fun results ->
                        results.Values
                        |> Seq.map (fun i ->
                            {
                                Region = ""
                                Symbol = i.symbol |> Option.defaultValue ""
                                SecurityName = i.description |> Option.defaultValue ""
                                Exchange = i.exchange |> Option.defaultValue ""
                                SecurityType = i.assetType |> Option.defaultValue ""
                            }
                        )
                        |> Seq.sortBy (fun r ->
                            match r.Exchange with
                            | "Pink Sheet" -> 1
                            | _ -> 0
                        )
                        |> Seq.sortBy (fun r ->
                            match r.SecurityType with
                            | "EQUITY" -> 0
                            | "OPTION" -> 1
                            | "MUTUAL_FUND" -> 2
                            | "ETF" -> 3
                            | "INDEX" -> 4
                            | "CASH_EQUIVALENT" -> 5
                            | "FIXED_INCOME" -> 6
                            | "CURRENCY" -> 7
                            | _ -> 8
                        )
                        |> Seq.truncate limit
                        |> Seq.toArray
                    )
            }
            
            return! execIfConnectedToBrokerage state connectedStateFunc
        }

        member this.GetQuote (user: UserState) (ticker: Ticker) = task {
            let execFunc user = task {
                let function' = $"marketdata/{ticker.Value}/quotes"

                let! response = this.CallApi<Dictionary<string, StockQuote>>(user, function', HttpMethod.Get)

                match response with
                | Error error -> return Error error
                | Ok response ->
                    match response.TryGetValue(ticker.Value) with
                    | true, quote -> return Ok quote
                    | false, _ -> return "Could not find quote for ticker" |> ServiceError |> Error
            }
            
            return! execIfConnectedToBrokerage user execFunc
        }

        member this.GetQuotes (user: UserState) (tickers: Ticker seq) = task {
            
            let func state = task {
                let commaSeparated = String.concat "," (tickers |> Seq.map _.Value)
                let function' = $"marketdata/quotes?symbol={commaSeparated}"

                let! result = this.CallApi<Dictionary<string, StockQuote>>(state, function', HttpMethod.Get)

                // doing this conversion as I can't seem to find a way for system.text.json to support dictionary deserialization where
                // the key is something other than string. If I put Ticker in there, I get a runtime error. Tried to use converters
                // but that did not work, seems like you had to add dictionary converter and it got ugly pretty quickly
                return
                    match result with
                    | Error error -> Error error
                    | Ok result ->
                        Ok (result |> Seq.map (fun (KeyValue(k, v)) -> (Ticker k, v)) |> dict)
            }
            
            return! execIfConnectedToBrokerage user func
        }

        member this.GetOptions (state: UserState) (ticker: Ticker) (expirationDate: DateTimeOffset option) (strikePrice: decimal option) (contractType: string option) = task {
            
            let execFunc state = task {
                let parameters =
                    [
                        $"symbol={ticker.Value}" |> Some
                        match contractType with Some contractType -> $"contractType={contractType}" |> Some | None -> None
                        match expirationDate with Some expirationDate -> $"fromDate=" + expirationDate.ToString("yyyy-MM-dd") |> Some | None -> None
                        match expirationDate with Some expirationDate -> $"toDate=" + expirationDate.ToString("yyyy-MM-dd") |> Some | None -> None
                        match strikePrice with Some strikePrice -> $"strike={strikePrice}" |> Some | None -> None
                    ]
                    |> List.choose id
                    |> String.concat "&"

                let function' = $"marketdata/chains?{parameters}"
                
                let! chainResponse = this.CallApi<OptionChain>(state, function', HttpMethod.Get)
                
                return chainResponse
                |> Result.map (fun chain ->
                    let toOptionDetails (map: Dictionary<string, Dictionary<string, OptionDescriptor[]>>) underlyingPrice =
                        map
                        |> Seq.collect (fun (KeyValue(_, v)) -> v.Values |> Seq.collect id)
                        |> Seq.map (fun d ->
                            let detail = core.fs.Adapters.Options.OptionDetail(d.symbol.Value, d.putCall.Value.ToLower(), d.description.Value)
                            detail.Ask <- d.ask
                            detail.Bid <- d.bid
                            detail.StrikePrice <- d.strikePrice
                            detail.Volume <- d.totalVolume
                            detail.OpenInterest <- d.openInterest
                            detail.ParsedExpirationDate <- d.ExpirationDate |> Some
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
                        
                    core.fs.Adapters.Options.OptionChain(chain.symbol.Value, chain.volatility, chain.numberOfContracts, options)
                )
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
                
                let function' = $"marketdata/{ticker.Value}/pricehistory?periodType=month&frequencyType={frequencyType}&startDate={startUnix}&endDate={endUnix}"

                let! response = this.CallApi<PriceHistoryResponse>(
                    state,
                    function',
                    HttpMethod.Get
                )

                match response with
                | Error error -> return Error error
                | Ok prices ->
                    let candles = defaultArg prices.candles [||]

                    let payload =
                        candles
                        |> Array.map (
                            fun c -> PriceBar(DateTimeOffset.FromUnixTimeMilliseconds(c.datetime), c.open', c.high, c.low, c.close,c.volume)
                        )

                    if Array.isEmpty payload then
                        return $"No candles for historical prices for {ticker.Value}" |> ServiceError |> Error
                    else
                        return PriceBars(payload) |> Ok
            }
            
            return! execIfConnectedToBrokerage state execFunc
        }
