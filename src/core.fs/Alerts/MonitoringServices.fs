module core.fs.Alerts.MonitoringServices

open System
open System.Collections.Generic
open System.Threading
open core.Account
open core.Shared
open core.fs
open core.fs.Accounts
open core.fs.Adapters.Brokerage
open core.fs.Adapters.Email
open core.fs.Adapters.Logging
open core.fs.Adapters.Stocks
open core.fs.Adapters.Storage
open core.fs.Alerts
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Stocks

[<Struct>]
type private PatternCheck = {
    ticker: Ticker
    listNames: string list
    user: UserId
}

[<Struct>]
type private StopLossCheck = {
    ticker: Ticker
    stopPrice: decimal
    isShort: bool
    user: UserState
}

type private WeeklyPatternCheckResult =
    | Success of Ticker * Pattern list
    | Failure of Ticker

let private toEmailData (marketHours:IMarketHours) (alert:TriggeredAlert) =
        let formattedValue format (value:decimal) =
            match format with
            | ValueFormat.Currency -> value.ToString("C2", CultureUtils.DefaultCulture)
            | ValueFormat.Percentage -> value.ToString("P1", CultureUtils.DefaultCulture)
            | ValueFormat.Number -> value.ToString("N2", CultureUtils.DefaultCulture)
            | ValueFormat.Boolean -> value.ToString()

        {|
            ticker = alert.ticker.Value
            value = alert.triggeredValue |> formattedValue alert.valueFormat
            description = alert.description
            sourceLists = alert.sourceLists
            sourceList = alert.sourceLists.Head
            time = alert.``when`` |> marketHours.ToMarketTime |> fun x -> x.ToString("HH:mm") + " ET"
        |}

let private toEmailAlert marketHours alertGroup =
    {| identifier = alertGroup |> fst; alertCount = alertGroup |> snd |> Seq.length; alerts = alertGroup |> snd |> Seq.map (toEmailData marketHours)  |}

let generateEmailDataPayloadForAlertsWithGroupingFunction title marketHours alertDiffs groupingFunc alerts=
    let groups =
        alerts
        |> Seq.groupBy groupingFunc
        |> Seq.map (toEmailAlert marketHours)
        
    {| alertGroups = groups; alertDiffs = alertDiffs; title = title |};

let generateEmailDataPayloadForAlerts title marketHours alertDiffs = generateEmailDataPayloadForAlertsWithGroupingFunction title marketHours alertDiffs _.identifier

type StopLossMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, container:StockAlertContainer, portfolio:IPortfolioStorage, logger:ILogger) =

    let checks = List<StopLossCheck>()

    let runStopLossCheck (logger:ILogger) (check:StopLossCheck) = async {
        let! priceResponse = brokerage.GetQuote check.user check.ticker |> Async.AwaitTask

        match priceResponse with
        | Error err ->
            logger.LogError($"Could not get price for {check.ticker}: {err.Message}")
            return None

        | Ok response ->

            let trigger =
                match check.isShort with
                | true -> response.Price > check.stopPrice
                | false -> response.Price < check.stopPrice

            match trigger with
            | true -> TriggeredAlert.StopPriceAlert check.ticker response.Price check.stopPrice DateTimeOffset.UtcNow (check.user.Id |> UserId) |> container.Register
            | false -> container.Deregister check.ticker Constants.StopLossIdentifier (check.user.Id |> UserId)

            return Some check
    }

    let generateStopLossChecksForUser (logger:ILogger) userId = task {

        logger.LogInformation($"Running stop loss check for {userId}")

        let! user = accounts.GetUser(userId) |> Async.AwaitTask

        match user with
        | None ->
            logger.LogError $"Unable to find user {userId}"
            return []
        | Some user ->
            logger.LogInformation $"Found user {userId}"

            match user.State.ConnectedToBrokerage with
            | false ->
                logger.LogInformation($"User {userId} is not connected to a brokerage")
                return []
            | true ->
                let! stockPositions = userId |> portfolio.GetStockPositions

                let checks =
                    stockPositions
                    |> Seq.filter (fun s -> s.IsOpen && s.HasStopPrice)
                    |> Seq.map (fun p -> {ticker=p.Ticker; stopPrice=p.StopPrice.Value; user=user.State; isShort = p.StockPositionType = Short })
                    |> Seq.toList

                logger.LogInformation("done")

                return checks
    }

    interface IApplicationService

    member _.RunStopLossMonitoring() = task {

        match checks.Count with
        | 0 ->
            container.AddNotice("Generating stop loss checks")
            container.ClearStopLossAlert()

            let! users = accounts.GetUserEmailIdPairs()

            let! _ =
                users
                |> Seq.map (fun pair -> async {
                    let! generatedChecks = generateStopLossChecksForUser logger pair.Id |> Async.AwaitTask
                    checks.AddRange(generatedChecks)
                })
                |> Async.Parallel

            container.AddNotice($"Generated {checks.Count} stop loss checks")
        | _ ->
            ()

        let! resolvedChecks =
            checks
            |> Seq.map (runStopLossCheck logger)
            |> Async.Sequential
            |> Async.StartAsTask

        resolvedChecks |> Array.choose id |> Array.iter (fun c -> checks.Remove(c) |> ignore)

        match checks.Count with
        | 0 -> container.AddNotice("Stop loss checks completed")
        | _ -> container.AddNotice("Stop loss checks pending, remaining " + checks.Count.ToString())
    }

type PatternMonitoringService(
    accounts:IAccountStorage,
    brokerage:IBrokerage,
    container:StockAlertContainer,
    marketHours:IMarketHours,
    portfolio:IPortfolioStorage,
    logger:ILogger) =

    let listOfChecks = List<PatternCheck>()
    let priceCache = Dictionary<Ticker, PriceBars>()

    let generateAlertListForUser (emailIdPair:EmailIdPair) = async {
        let! user = emailIdPair.Id |> accounts.GetUser |> Async.AwaitTask

        match user with
        | None ->
            return Seq.empty<PatternCheck>
        | Some user ->
            let userId = user.State.Id |> UserId
            let! ownedStocks = emailIdPair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
            let portfolioList =
                ownedStocks
                |> Seq.filter (_.IsOpen)
                |> Seq.map (_.Ticker)
                |> Seq.map (fun t -> {ticker=t; listNames=[Constants.PortfolioIdentifier]; user=userId})

            let! pendingPositions = emailIdPair.Id |> portfolio.GetPendingStockPositions |> Async.AwaitTask
            let pendingList =
                pendingPositions
                |> Seq.filter (fun p -> p.State.IsClosed |> not)
                |> Seq.map (_.State.Ticker)
                |> Seq.map (fun t -> {ticker=t; listNames=[Constants.PendingIdentifier]; user=userId})

            let! lists = emailIdPair.Id |> portfolio.GetStockLists |> Async.AwaitTask
            let stockList =
                lists
                |> Seq.filter (_.State.ContainsTag(Constants.MonitorTagPattern))
                |> Seq.map (fun l -> l.State.Tickers |> Seq.map (fun t -> {ticker=t.Ticker; listNames=[l.State.Name]; user=userId}))
                |> Seq.concat

            // create a map of all the tickers we are checking so we can remove duplicates, and we want to prefer portfolio list entries
            // over pending list entries over stock list entries
            let tickerMap = Dictionary<Ticker, PatternCheck>()

            let addTicker (ticker: Ticker) (check:PatternCheck) =
                match tickerMap.TryGetValue(ticker) with
                | true, _ -> tickerMap[ticker] <- {check with listNames = check.listNames @ tickerMap[ticker].listNames}
                | _ -> tickerMap.Add(ticker, check)

            portfolioList |> Seq.iter (fun check -> addTicker check.ticker check)
            pendingList |> Seq.iter (fun check -> addTicker check.ticker check)
            stockList |> Seq.iter (fun check -> addTicker check.ticker check)

            return tickerMap.Values
    }

    let generatePatternMonitoringChecks() = task {
        listOfChecks.Clear()

        let! users = accounts.GetUserEmailIdPairs()

        let! alertListOps =
            users
            |> Seq.map generateAlertListForUser
            |> Async.Parallel
            |> Async.StartAsTask

        let allAlerts = alertListOps |> Seq.concat

        listOfChecks.AddRange(allAlerts)

        container.AddNotice($"Pattern check generator added {listOfChecks.Count}")
    }

    let getPrices (logger:ILogger) (user:UserState) ticker = task {

        match priceCache.TryGetValue(ticker) with
        | true, prices ->
            return prices |> Ok
        | _ ->
            let start = marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-365)) |> Some
            let ``end`` = marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow) |> Some

            let! prices = brokerage.GetPriceHistory user ticker PriceFrequency.Daily start ``end``

            match prices with
            | Error err ->
                logger.LogError($"Pattern monitor could not get price history for {ticker}: {err.Message}")
            | Ok response ->
                // see how many bars did we get, I suspect we get only one bar from time to time
                match response.Length with
                | x when x <= 1 -> logger.LogWarning($"Pattern monitor got only {response.Length} bars for {ticker}")
                | _ -> ()
                priceCache.Add(ticker, response)

            return prices
    }

    let runCheck (logger:ILogger) (alertCheck:PatternCheck) = async {

        let! user = accounts.GetUser alertCheck.user |> Async.AwaitTask
        
        match user with
        | None -> return None
        | Some user ->
            match user.State.ConnectedToBrokerage with
            | false -> return None
            | true ->
                let! priceResponse = getPrices logger user.State alertCheck.ticker |> Async.AwaitTask

                match priceResponse with
                | Error _ -> return None
                | Ok prices ->

                    PatternDetection.availablePatterns
                    |> Seq.iter( fun pattern ->
                        container.Deregister alertCheck.ticker pattern alertCheck.user
                    )

                    let patterns = PatternDetection.generate prices

                    patterns |> List.iter (fun p ->
                        let alert = TriggeredAlert.PatternAlert p alertCheck.ticker alertCheck.listNames DateTimeOffset.UtcNow alertCheck.user
                        container.Register alert
                    )

                    return Some (alertCheck,patterns.Length)
    }

    let runThroughMonitoringChecks logger = task {
        let startingNumberOfChecks = listOfChecks.Count
        
        match startingNumberOfChecks with
        | 0 ->
            container.AddNotice("Pattern monitoring checks starting")
            do! generatePatternMonitoringChecks()
        | _ ->
            container.AddNotice("Pattern monitoring checks continuing with " + startingNumberOfChecks.ToString())
        
        let! checks =
            listOfChecks
            |> Seq.map (runCheck logger)
            |> Async.Sequential
            |> Async.StartAsTask

        let completedChecksWithCounts = checks |> Seq.choose id

        let completedChecks = completedChecksWithCounts |> Seq.map fst

        completedChecks |> Seq.iter (fun kp -> listOfChecks.Remove(kp) |> ignore)

        let totalPatternsFoundCount = completedChecksWithCounts |> Seq.map snd |> Seq.sum

        match listOfChecks.Count with
        | 0 ->
            priceCache.Clear()
            if startingNumberOfChecks > 0 then
                container.AddNotice($"Pattern monitoring checks completed with {totalPatternsFoundCount} patterns found")
        | _ ->
            container.AddNotice($"{completedChecks |> Seq.length} pattern monitoring checks completed, {listOfChecks.Count} remaining")
    }

    interface IApplicationService

    member _.RunPatternMonitoring() = task {

        try
            do! runThroughMonitoringChecks logger
        with
            | ex ->
                logger.LogError("Failed while running alert monitor, will sleep: " + ex.ToString())
                container.AddNotice("Failed while running alert monitor: " + ex.Message)
    }


type WeeklyMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, emails:IEmailService, marketHours:IMarketHours, portfolio:IPortfolioStorage, logger:ILogger) =

    let tickersToCheck = Dictionary<UserState, HashSet<Ticker>>()
    let weeklyAlertsDiscovered = Dictionary<UserState, List<TriggeredAlert>>()

    let loadTickersToCheckForUser (logger:ILogger) (pair:EmailIdPair) = async {
        let! user = pair.Id |> accounts.GetUser |> Async.AwaitTask
        match user with
        | None -> logger.LogError($"Could not find user {pair.Id}")
        | Some user ->
            let! stocks = pair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
            let tickersFromPositions = stocks |> Seq.filter _.IsOpen |> Seq.map _.Ticker
            let! lists = pair.Id |> portfolio.GetStockLists |> Async.AwaitTask
            let tickersFromLists =
                lists
                |> Seq.filter _.State.ContainsTag(Constants.MonitorTagPattern)
                |> Seq.map (fun l -> l.State.Tickers |> Seq.map _.Ticker)
                |> Seq.concat

            let set = HashSet<Ticker>(tickersFromLists |> Seq.append tickersFromPositions);

            tickersToCheck[user.State] <- set;
    }

    let loadTickersToCheck (logger:ILogger) = async {
        let! users = accounts.GetUserEmailIdPairs() |> Async.AwaitTask

        do!
            users
            |> Seq.map (loadTickersToCheckForUser logger)
            |> Async.Sequential
            |> Async.Ignore
    }

    let runCheckForUserTicker (logger:ILogger) user ticker = async {
        let! prices = brokerage.GetPriceHistory user ticker PriceFrequency.Weekly None None |> Async.AwaitTask

        return
            match prices with
            | Error err ->
                logger.LogError($"Weekly job could not get price history for {ticker}: {err.Message}")
                WeeklyPatternCheckResult.Failure ticker
            | Ok bars ->
                let patterns = PatternDetection.generate bars
                (ticker, patterns) |> WeeklyPatternCheckResult.Success
    }

    let runCheckForUser (logger:ILogger) (user:UserState) (tickers: HashSet<Ticker>) = async {

        if weeklyAlertsDiscovered.ContainsKey(user) |> not then
            weeklyAlertsDiscovered.Add(user, List<TriggeredAlert>())

        let! work =
            tickers
            |> Seq.map (runCheckForUserTicker logger user)
            |> Async.Sequential

        let succeeded = work |> Seq.choose (function WeeklyPatternCheckResult.Success (t,p) -> Some (t,p) | _ -> None) |> Seq.toList
        let failed = work |> Seq.choose (function WeeklyPatternCheckResult.Failure x -> Some x | _ -> None) |> Seq.toList

        logger.LogInformation($"Weekly pattern check for {user.Id} successfully checked {succeeded.Length} tickers, and failed for {failed.Length} tickers")
        
        succeeded
        |> List.map (fun (ticker, patterns) ->
            patterns
            |> List.map (fun p -> TriggeredAlert.PatternAlert p ticker ["Watchlist"] DateTimeOffset.UtcNow (user.Id |> UserId))
        )
        |> List.concat
        |> weeklyAlertsDiscovered[user].AddRange

        let removed = succeeded |> Seq.map fst |> Seq.map tickers.Remove |> Seq.map (fun b -> if b then 1 else 0) |> Seq.sum

        logger.LogInformation($"Weekly pattern check for {user.Id} removed {removed} tickers from the check list")
    }

    let runChecks (logger:ILogger) = async {
        do!
            tickersToCheck
            |> Seq.map (fun pair -> runCheckForUser logger pair.Key pair.Value)
            |> Async.Sequential
            |> Async.Ignore
    }

    let sendEmails (logger:ILogger) = async {
        logger.LogInformation $"Weekly pattern check emails discovered for {weeklyAlertsDiscovered.Count} users"

        let! _ =
            weeklyAlertsDiscovered
            |> Seq.filter (fun pair -> pair.Value.Count > 0)
            |> Seq.map (fun pair -> async {

                let recipient = Recipient(email=pair.Key.Email, name=pair.Key.Name)

                do!
                    pair.Value
                    |> generateEmailDataPayloadForAlertsWithGroupingFunction "NGTD: Weekly Patterns" marketHours [] (fun a -> "Weekly " + a.identifier)
                    |> emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.Alerts
                    |> Async.AwaitTask
            })
            |> Async.Sequential

        weeklyAlertsDiscovered.Clear()
    }

    let tickersToCheckCount() = tickersToCheck |> Seq.map _.Value |> Seq.map _.Count |> Seq.sum

    let isWeekend() =
        match marketHours.ToMarketTime(DateTimeOffset.UtcNow).DayOfWeek with
        | DayOfWeek.Saturday -> true
        | DayOfWeek.Sunday -> true
        | _ -> false

    interface IApplicationService

    member _.Execute forceRun = task {
        match isWeekend() || forceRun with
        | false ->
            logger.LogInformation("Not running weekly pattern check because it is not Friday or the weekend")
        | true ->
            logger.LogInformation("Running weekly pattern check")

            if tickersToCheckCount() = 0 then
                weeklyAlertsDiscovered.Clear()
                logger.LogInformation("No tickers to check, loading them")
                do! loadTickersToCheck logger

            logger.LogInformation($"Running {tickersToCheckCount()} checks")
            let! _ = runChecks logger
            match tickersToCheckCount() with
            | 0 ->
                logger.LogInformation("Sending emails")
                do! sendEmails logger
            | _ ->
                logger.LogInformation($"Checks remaining: {tickersToCheckCount()}")
    }

    // run this Saturday morning then all the data has settled
    // Friday evenings often brokerage data is under some sort of maintenance
    member _.NextRunTime (now:DateTimeOffset) =
        match tickersToCheckCount() with
        | x when x > 0 -> now.Add(TimeSpan.FromMinutes(1.0))
        | _ ->
            let marketTime = marketHours.ToMarketTime(now)

            let dayOffset =
                match marketTime.DayOfWeek with
                | DayOfWeek.Saturday -> 7
                | DayOfWeek.Sunday -> 6
                | _ -> (DayOfWeek.Saturday |> int) - (int marketTime.DayOfWeek)

            let nextSaturday = marketTime.Date.AddDays(float dayOffset).AddHours(10) // 10am eastern

            marketHours.ToUniversalTime(nextSaturday)


type AlertEmailService(
        accounts:IAccountStorage,
        blobStorage:IBlobStorage,
        container:StockAlertContainer,
        emails:IEmailService,
        logger:ILogger,
        marketHours:IMarketHours) =
   
    let processAlerts (user:User) alerts = async {
        
        let toAlertCountPairs (sequence:TriggeredAlert seq) =
            sequence |> Seq.groupBy (_.identifier) |> Seq.map (fun (k,v) -> k, v |> Seq.length) |> Seq.toList
                
        let key = $"{user.State.Id}/" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "/alerts.json"
        
        let! fromStorage = blobStorage.Get<TriggeredAlert seq>(key) |> Async.AwaitTask
        
        let diffCount =
            match fromStorage with
            | None -> []
            | Some fromStorage ->
                let previousCounts = fromStorage |> toAlertCountPairs
                let currentCounts = alerts |> toAlertCountPairs
                
                currentCounts |> List.map (
                    fun (identifier, count) ->
                        let previousValue = previousCounts |> List.tryFind (fun (k2,_) -> k2 = identifier) |> Option.defaultValue (identifier,0) |> snd
                        {| identifier = identifier; change = count - previousValue; previous = previousValue; current = count |}
                )
            
        if fromStorage = None then do! blobStorage.Save(key, alerts) |> Async.AwaitTask
        
        // only email if we are at the end of the day
        if marketHours.IsMarketOpen(DateTimeOffset.UtcNow) |> not then
            let recipient = Recipient(email=user.State.Email, name=user.State.Name)
            
            logger.LogInformation($"Sending {alerts |> Seq.length} alerts to {recipient}")
            do!
                alerts
                |> generateEmailDataPayloadForAlerts "NGTD: Alerts" marketHours diffCount
                |> emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.Alerts
                |> Async.AwaitTask
    }

    interface IApplicationService

    member _.Execute() = task {

            let! users = accounts.GetUserEmailIdPairs()

            do!
                users
                |> Seq.map (fun emailIdPair -> async {
                    let! user = accounts.GetUser emailIdPair.Id |> Async.AwaitTask
                    match user with
                    | None -> logger.LogError $"User {emailIdPair.Id} not found"
                    | Some user ->
                        let alerts = container.GetAlerts emailIdPair.Id
                        match alerts |> Seq.isEmpty with
                        | true -> ()
                        | false -> do! processAlerts user alerts;
                })
                |> Async.Sequential
                |> Async.Ignore

            container.AddNotice "Emails sent"
        }
