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
    listName: string
    user: UserState
}

[<Struct>]
type private StopLossCheck = {
    ticker: Ticker
    stopPrice: decimal
    isShort: bool
    user: UserState
}

type private WeeklyUpsideCheckResult =
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
            ticker = alert.ticker.Value;
            value = alert.triggeredValue |> formattedValue alert.valueFormat;
            description = alert.description;
            sourceList = alert.sourceList;
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

let private _patternMonitorTimes = [
    TimeOnly.Parse("09:45")
    TimeOnly.Parse("11:15")
    TimeOnly.Parse("13:05")
    TimeOnly.Parse("14:35")
    TimeOnly.Parse("15:45")
    TimeOnly.Parse("16:15")
]
let nextPatternMonitoringRun referenceTimeUtc (marketHours:IMarketHours) =
    let easternTime = marketHours.ToMarketTime(referenceTimeUtc)

    let candidates =
        _patternMonitorTimes
        |> List.map (fun t -> DateTimeOffset(easternTime.Date.Add(t.ToTimeSpan())))

    let candidatesInFuture =
        candidates
        |> List.filter (fun t -> t > easternTime)
        |> List.map marketHours.ToUniversalTime

    match candidatesInFuture with
    | head :: _ -> head
    | _ ->
        // markets are closed if we get here, so jump to the next day
        let nextDayOffset =
            match easternTime.DayOfWeek with
            | DayOfWeek.Friday -> 3
            | DayOfWeek.Saturday -> 2
            | _ -> 1

        let nextDay = candidates.Head.AddDays(nextDayOffset)

        marketHours.ToUniversalTime(nextDay);

type StopLossMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, container:StockAlertContainer, portfolio:IPortfolioStorage, marketHours:IMarketHours) =

    // need to decide how I will log these
    let marketStartTime = TimeOnly(9, 30, 0)
    let marketEndTime = TimeOnly(16, 0, 0)
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

    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {

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
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (runStopLossCheck logger)
            |> Async.Sequential
            |> Async.StartAsTask

        resolvedChecks |> Array.choose id |> Array.iter (fun c -> checks.Remove(c) |> ignore)

        match checks.Count with
        | 0 -> container.AddNotice("Stop loss checks completed")
        | _ -> container.AddNotice("Stop loss checks pending, remaining " + checks.Count.ToString())
    }

    member _.NextRunTime (now:DateTimeOffset) =

        match checks.Count with
        | x when x > 0 -> now.Add(TimeSpan.FromMinutes(1.0))
        | _ ->
            let nowInEasterTimezone = marketHours.ToMarketTime(now)
            let marketStartTimeInEastern = DateTimeOffset(nowInEasterTimezone.Date.Add(marketStartTime.ToTimeSpan()))

            let nextScan =
                match TimeOnly.FromTimeSpan(nowInEasterTimezone.TimeOfDay) with
                | t when t < marketStartTime -> marketStartTimeInEastern
                | t when t > marketEndTime -> marketStartTimeInEastern.AddDays(1)
                | _ -> nowInEasterTimezone.AddMinutes(5)

            let adjustedScanTime =
                match nextScan.DayOfWeek with
                | DayOfWeek.Saturday -> nextScan.AddDays(2)
                | DayOfWeek.Sunday -> nextScan.AddDays(1)
                | _ -> nextScan

            marketHours.ToUniversalTime(adjustedScanTime)

type PatternMonitoringService(accounts:IAccountStorage,brokerage:IBrokerage,container:StockAlertContainer,marketHours:IMarketHours,portfolio:IPortfolioStorage) =

    let mutable nextPatternMonitoringRunDateTime = DateTimeOffset.MinValue
    let listOfChecks = List<PatternCheck>()
    let priceCache = Dictionary<Ticker, PriceBars>()

    let generateAlertListForUser (emailIdPair:EmailIdPair) = async {
        let! user = emailIdPair.Id |> accounts.GetUser |> Async.AwaitTask

        match user with
        | None ->
            return Seq.empty<PatternCheck>
        | Some user ->
            let! ownedStocks = emailIdPair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
            let portfolioList =
                ownedStocks
                |> Seq.filter (_.IsOpen)
                |> Seq.map (_.Ticker)
                |> Seq.map (fun t -> {ticker=t; listName=Constants.PortfolioIdentifier; user=user.State})

            let! pendingPositions = emailIdPair.Id |> portfolio.GetPendingStockPositions |> Async.AwaitTask
            let pendingList =
                pendingPositions
                |> Seq.filter (fun p -> p.State.IsClosed |> not)
                |> Seq.map (_.State.Ticker)
                |> Seq.map (fun t -> {ticker=t; listName=Constants.PendingIdentifier; user=user.State})

            let! lists = emailIdPair.Id |> portfolio.GetStockLists |> Async.AwaitTask
            let stockList =
                lists
                |> Seq.filter (_.State.ContainsTag(Constants.MonitorTagPattern))
                |> Seq.map (fun l -> l.State.Tickers |> Seq.map (fun t -> {ticker=t.Ticker; listName=l.State.Name; user=user.State}))
                |> Seq.concat

            // create a map of all the tickers we are checking so we can remove duplicates, and we want to prefer portfolio list entries
            // over pending list entries over stock list entries
            let tickerMap = Dictionary<Ticker, PatternCheck>()

            let addTicker (ticker: Ticker) (check:PatternCheck) =
                match tickerMap.TryGetValue(ticker) with
                | true, _ -> ()
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

        nextPatternMonitoringRunDateTime <- nextPatternMonitoringRun DateTimeOffset.UtcNow marketHours

        container.AddNotice(
             $"Pattern check generator added {listOfChecks.Count}, next run at {marketHours.ToMarketTime(nextPatternMonitoringRunDateTime)}"
        )
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
                priceCache.Add(ticker, response)

            return prices
    }

    let runCheck (logger:ILogger) (alertCheck:PatternCheck) = async {

        let! priceResponse = getPrices logger alertCheck.user alertCheck.ticker |> Async.AwaitTask

        match priceResponse with
        | Error _ -> return None
        | Ok prices ->

            let userId = alertCheck.user.Id |> UserId

            PatternDetection.availablePatterns
            |> Seq.iter( fun pattern ->
                container.Deregister alertCheck.ticker pattern userId
            )

            let patterns = PatternDetection.generate prices

            patterns |> List.iter (fun p ->
                let alert = TriggeredAlert.PatternAlert p alertCheck.ticker alertCheck.listName DateTimeOffset.UtcNow userId
                container.Register alert
            )

            return Some (alertCheck,patterns.Length)
    }

    let runThroughMonitoringChecks logger (cancellationToken:CancellationToken) = task {
        let now = DateTimeOffset.UtcNow

        if now > nextPatternMonitoringRunDateTime then
            container.AddNotice("Running pattern monitoring checks")
            do! generatePatternMonitoringChecks()

        let startingNumberOfChecks = listOfChecks.Count

        let! checks =
            listOfChecks
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
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
            container.AddNotice($"Pattern monitoring checks completed, {listOfChecks.Count} remaining")
    }

    let monitoringFrequency = TimeSpan.FromMinutes(1)

    interface IApplicationService

    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {

        try
            do! runThroughMonitoringChecks logger cancellationToken
        with
            | ex ->
                logger.LogError("Failed while running alert monitor, will sleep: " + ex.ToString())
                container.AddNotice("Failed while running alert monitor: " + ex.Message)
    }

    member _.NextRunTime (now:DateTimeOffset) =
        now.Add(monitoringFrequency)


type WeeklyUpsideMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, emails:IEmailService, marketHours:IMarketHours, portfolio:IPortfolioStorage) =

    let tickersToCheck = Dictionary<UserState, HashSet<Ticker>>()
    let weeklyUpsidesDiscovered = Dictionary<UserState, List<TriggeredAlert>>()

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

    let loadTickersToCheck (logger:ILogger) (cancellationToken:CancellationToken) = async {
        let! users = accounts.GetUserEmailIdPairs() |> Async.AwaitTask

        do!
            users
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (loadTickersToCheckForUser logger)
            |> Async.Sequential
            |> Async.Ignore
    }

    let runCheckForUserTicker (logger:ILogger) (_:CancellationToken) user ticker = async {
        let! prices = brokerage.GetPriceHistory user ticker PriceFrequency.Weekly None None |> Async.AwaitTask

        return
            match prices with
            | Error err ->
                logger.LogError($"Weekly job could not get price history for {ticker}: {err.Message}")
                WeeklyUpsideCheckResult.Failure ticker
            | Ok bars ->
                let patterns =
                    [
                        PatternDetection.upsideReversal(bars)
                        PatternDetection.downsideReversal(bars)
                    ]
                    |> List.choose id
                (ticker, patterns) |> WeeklyUpsideCheckResult.Success
    }

    let runCheckForUser (logger:ILogger) (cancellationToken:CancellationToken) (user:UserState) (tickers: HashSet<Ticker>) = async {

        if weeklyUpsidesDiscovered.ContainsKey(user) |> not then
            weeklyUpsidesDiscovered.Add(user, List<TriggeredAlert>())

        let! work =
            tickers
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (runCheckForUserTicker logger cancellationToken user)
            |> Async.Sequential

        let succeeded = work |> Seq.choose (function WeeklyUpsideCheckResult.Success (t,p) -> Some (t,p) | _ -> None) |> Seq.toList
        let failed = work |> Seq.choose (function WeeklyUpsideCheckResult.Failure x -> Some x | _ -> None) |> Seq.toList

        logger.LogInformation($"Weekly upside reversal check for {user.Id} successfully checked {succeeded.Length} tickers, and failed for {failed.Length} tickers")
        
        succeeded
        |> List.map (fun (ticker, patterns) ->
            patterns
            |> List.map (fun p -> TriggeredAlert.PatternAlert p ticker "Watchlist" DateTimeOffset.UtcNow (user.Id |> UserId))
        )
        |> List.concat
        |> weeklyUpsidesDiscovered[user].AddRange

        let removed = succeeded |> Seq.map fst |> Seq.map tickers.Remove |> Seq.map (fun b -> if b then 1 else 0) |> Seq.sum

        logger.LogInformation($"Weekly upside reversal check for {user.Id} removed {removed} tickers from the check list")
    }

    let runChecks (logger:ILogger) (cancellationToken:CancellationToken) = async {
        do!
            tickersToCheck
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (fun pair -> runCheckForUser logger cancellationToken pair.Key pair.Value)
            |> Async.Sequential
            |> Async.Ignore
    }

    let sendEmails (logger:ILogger) (cancellationToken:CancellationToken) = async {
        logger.LogInformation $"Weekly upside reversal emails discovered for {weeklyUpsidesDiscovered.Count} users"

        let! _ =
            weeklyUpsidesDiscovered
            |> Seq.filter (fun pair -> pair.Value.Count > 0)
            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
            |> Seq.map (fun pair -> async {

                let recipient = Recipient(email=pair.Key.Email, name=pair.Key.Name)

                do!
                    pair.Value
                    |> generateEmailDataPayloadForAlertsWithGroupingFunction "NGTD: Weekly Alerts" marketHours [] (fun a -> "Weekly " + a.identifier)
                    |> emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.Alerts
                    |> Async.AwaitTask
            })
            |> Async.Sequential

        weeklyUpsidesDiscovered.Clear()
    }

    let tickersToCheckCount() = tickersToCheck |> Seq.map _.Value |> Seq.map _.Count |> Seq.sum

    let isWeekend() =
        match marketHours.ToMarketTime(DateTimeOffset.UtcNow).DayOfWeek with
        | DayOfWeek.Saturday -> true
        | DayOfWeek.Sunday -> true
        | _ -> false

    interface IApplicationService

    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {

        match isWeekend() with
        | false ->
            logger.LogInformation("Not running weekly upside reversal check because it is not Friday or the weekend")
        | true ->
            logger.LogInformation("Running weekly upside reversal check")

            if tickersToCheckCount() = 0 then
                weeklyUpsidesDiscovered.Clear()
                logger.LogInformation("No tickers to check, loading them")
                do! loadTickersToCheck logger cancellationToken

            logger.LogInformation($"Running {tickersToCheckCount()} checks")
            let! _ = runChecks logger cancellationToken
            match tickersToCheckCount() with
            | 0 ->
                logger.LogInformation("Sending emails")
                do! sendEmails logger cancellationToken
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

    let runTimes =
        [
            TimeOnly.Parse("09:50")
            TimeOnly.Parse("16:20")
        ]

    let processAlerts (user:User) alerts = async {
        
        let toAlertCountPairs (sequence:TriggeredAlert seq) =
            sequence |> Seq.groupBy (_.identifier) |> Seq.map (fun (k,v) -> k, v |> Seq.length) |> Seq.toList
                
        let key = $"{user.State.Id}/" + DateTime.UtcNow.Date.ToString("yyyy-MM-dd") + "/alerts.json"
        
        let! fromStorage = blobStorage.Get<TriggeredAlert seq>(key) |> Async.AwaitTask
        
        let diffCount =
            match fromStorage with
            | null -> []
            | _ ->
                let previousCounts = fromStorage |> toAlertCountPairs
                let currentCounts = alerts |> toAlertCountPairs
                
                currentCounts |> List.map (
                    fun (identifier, count) ->
                        let previousValue = previousCounts |> List.tryFind (fun (k2,_) -> k2 = identifier) |> Option.defaultValue (identifier,0) |> snd
                        {| identifier = identifier; change = count - previousValue; previous = previousValue; current = count |}
                )
            
        if fromStorage = null then do! blobStorage.Save(key, alerts) |> Async.AwaitTask
                
        let recipient = Recipient(email=user.State.Email, name=user.State.Name)
       
        logger.LogInformation($"Sending {alerts |> Seq.length} alerts to {recipient}")
        do!
            alerts
            |> generateEmailDataPayloadForAlerts "NGTD: Alerts" marketHours diffCount
            |> emails.SendWithTemplate recipient Sender.NoReply EmailTemplate.Alerts
            |> Async.AwaitTask
    }

    interface IApplicationService

    member _.Execute (logger:ILogger) (cancellationToken:CancellationToken) = task {

            let! users = accounts.GetUserEmailIdPairs()

            do!
                users
                |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
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

    member _.NextRunTime (now:DateTimeOffset) =

        let eastern = marketHours.ToMarketTime(now);

        let nextTime =
            runTimes
            |> Seq.map (fun t -> eastern.Date.Add(t.ToTimeSpan()) |> DateTimeOffset)
            |> Seq.filter (fun t -> t > eastern)
            |> Seq.map marketHours.ToUniversalTime
            |> Seq.tryHead

        match nextTime with
        | Some t ->
            t
        | None ->
            let nextDay = eastern.Date.AddDays(1).Add(runTimes[0].ToTimeSpan())

            match nextDay.DayOfWeek with
            | DayOfWeek.Saturday -> nextDay.AddDays(2)
            | DayOfWeek.Sunday -> nextDay.AddDays(1)
            | _ -> nextDay
            |> fun d -> d |> DateTimeOffset
            |> marketHours.ToUniversalTime
