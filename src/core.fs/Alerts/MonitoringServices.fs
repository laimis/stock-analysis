module core.fs.Alerts.MonitoringServices

    open System
    open System.Collections.Generic
    open System.Threading
    open core.Account
    open core.Shared
    open core.fs
    open core.fs.Accounts
    open core.fs.Adapters.Brokerage
    open core.fs.Adapters.Logging
    open core.fs.Adapters.Stocks
    open core.fs.Adapters.Storage
    open core.fs.Services
    open core.fs.Stocks
        
    let private _patternMonitorTimes = [
        TimeOnly.Parse("09:45")
        TimeOnly.Parse("11:15")
        TimeOnly.Parse("13:05")
        TimeOnly.Parse("14:35")
        TimeOnly.Parse("15:45")
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
                match candidates.Head.DayOfWeek with
                | DayOfWeek.Friday -> 3
                | DayOfWeek.Saturday -> 2
                | _ -> 1
                
            let nextDay = candidates.Head.AddDays(nextDayOffset)
            
            marketHours.ToUniversalTime(nextDay);
        
    type StopLossMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, container:StockAlertContainer, portfolio:IPortfolioStorage, logger:ILogger, marketHours:IMarketHours) =
        
        // need to decide how I will log these
        let logInformation = logger.LogInformation
        let logError = logger.LogError
        let marketStartTime = TimeOnly(9, 30, 0)
        let marketEndTime = TimeOnly(16, 0, 0)
        let checks = List<StopLossCheck>()
        
        let runStopLossCheck (check:StopLossCheck) = async {
            let! priceResponse = brokerage.GetQuote check.user check.ticker |> Async.AwaitTask
            
            match priceResponse.Success with
            | None ->
                logError($"Could not get price for {check.ticker}: {priceResponse.Error.Value.Message}")
                return None
                
            | Some response ->
                
                let trigger =
                    match check.isShort with
                    | true -> response.Price > check.stopPrice
                    | false -> response.Price < check.stopPrice
                
                match trigger with    
                | true -> TriggeredAlert.StopPriceAlert check.ticker response.Price check.stopPrice DateTimeOffset.UtcNow (check.user.Id |> UserId) |> container.Register
                | false -> container.Deregister check.ticker Constants.StopLossIdentifier (check.user.Id |> UserId)
                
                return Some check
        }
        
        let generateStopLossChecksForUser userId = task {
            
            logInformation($"Running stop loss check for {userId}")
            
            let! user = accounts.GetUser(userId) |> Async.AwaitTask
            
            match user with
            | None ->
                logError $"Unable to find user {userId}"
                return []
            | Some user ->
                logInformation $"Found user {userId}"
                
                let! stockPositions = userId |> portfolio.GetStockPositions
                
                let checks =
                    stockPositions
                    |> Seq.filter (fun s -> s.IsOpen && s.HasStopPrice)
                    |> Seq.map (fun p -> {ticker=p.Ticker; stopPrice=p.StopPrice.Value; user=user.State; isShort = p.IsShort })
                    |> Seq.toList
                    
                logInformation("done")
                
                return checks
        }
            
        interface IApplicationService
        
        member _.Execute (cancellationToken:CancellationToken) = task {
            
            match checks.Count with
            | 0 ->
                container.AddNotice("Generating stop loss checks")
                container.SetStopLossCheckCompleted(false)
                container.ClearStopLossAlert()
                
                let! users = accounts.GetUserEmailIdPairs()
                
                let! _ =
                    users
                    |> Seq.map (fun pair -> async {
                        let! generatedChecks = generateStopLossChecksForUser pair.Id |> Async.AwaitTask
                        checks.AddRange(generatedChecks)
                    })
                    |> Async.Parallel
                    
                container.AddNotice($"Generated {checks.Count} stop loss checks")
            | _ ->
                ()
            
            let! resolvedChecks =
                checks
                |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
                |> Seq.map runStopLossCheck
                |> Async.Sequential
                |> Async.StartAsTask
                
            resolvedChecks |> Array.choose id |> Array.iter (fun c -> checks.Remove(c) |> ignore)
               
            match checks.Count with
            | 0 ->
                container.SetStopLossCheckCompleted(true)
                container.AddNotice("Stop loss checks completed")
            | _ ->
                container.AddNotice("Stop loss checks pending, remaining " + checks.Count.ToString())
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
        
    type PatternMonitoringService(accounts:IAccountStorage,brokerage:IBrokerage,container:StockAlertContainer,logger:ILogger,marketHours:IMarketHours,portfolio:IPortfolioStorage) =
        
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
                    |> Seq.map (fun t -> {ticker=t; listName="Portfolio"; user=user.State})
                    
                let! pendingPositions = emailIdPair.Id |> portfolio.GetPendingStockPositions |> Async.AwaitTask
                let pendingList =
                    pendingPositions
                    |> Seq.filter (fun p -> p.State.IsClosed |> not)
                    |> Seq.map (_.State.Ticker)
                    |> Seq.map (fun t -> {ticker=t; listName="Pending"; user=user.State})
                
                let! lists = emailIdPair.Id |> portfolio.GetStockLists |> Async.AwaitTask
                return lists
                |> Seq.filter (_.State.ContainsTag(Constants.MonitorTagPattern))
                |> Seq.map (fun l -> l.State.Tickers |> Seq.map (fun t -> {ticker=t.Ticker; listName=l.State.Name; user=user.State}))
                |> Seq.concat
                |> Seq.append portfolioList
                |> Seq.append pendingList
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
            
            container.ManualRunCompleted()
            
            container.AddNotice(
                 $"Alert check generator added {listOfChecks.Count}, next run at {marketHours.ToMarketTime(nextPatternMonitoringRunDateTime)}"
            )
        }
        
        let getPrices (user:UserState) ticker = task {
            
            match priceCache.TryGetValue(ticker) with
            | true, prices ->
                return ServiceResponse<PriceBars>(prices)
            | _ ->
                let start = marketHours.GetMarketStartOfDayTimeInUtc(DateTime.UtcNow.AddDays(-365))
                let ``end`` = marketHours.GetMarketEndOfDayTimeInUtc(DateTime.UtcNow)
                
                let! prices = brokerage.GetPriceHistory user ticker PriceFrequency.Daily start ``end``
                
                match prices.Success with
                | None ->
                    logger.LogError($"Could not get price history for {ticker}: {prices.Error.Value.Message}")
                | Some response ->
                    priceCache.Add(ticker, response)
                
                return prices
        }
        
        let runCheck (alertCheck:PatternCheck) = async {
            
            let! priceResponse = getPrices alertCheck.user alertCheck.ticker |> Async.AwaitTask
            
            match priceResponse.Success with
            | None ->
                logger.LogError($"Could not get price for {alertCheck.ticker}: {priceResponse.Error.Value.Message}")
                return None
            | Some prices ->
                
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
                
                logger.LogInformation($"Found {patterns.Length} patterns for {alertCheck.ticker}")
                
                return Some (alertCheck,patterns.Length)
        }
        
        let runThroughMonitoringChecks (cancellationToken:CancellationToken) = task {
            let now = DateTimeOffset.UtcNow
            
            if now > nextPatternMonitoringRunDateTime || container.ManualRunRequested() then
                container.AddNotice("Running pattern monitoring checks")
                container.SetListCheckCompleted(false)
                do! generatePatternMonitoringChecks()
                
            priceCache.Clear()
            
            let startingNumberOfChecks = listOfChecks.Count
            
            let! checks =
                listOfChecks
                |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
                |> Seq.map runCheck
                |> Async.Sequential
                |> Async.StartAsTask
                
            let completedChecksWithCounts = checks |> Seq.choose id
            
            let completedChecks = completedChecksWithCounts |> Seq.map fst
            
            completedChecks |> Seq.iter (fun kp ->
                listOfChecks.Remove(kp) |> ignore
            )
            
            let totalPatternsFoundCount = completedChecksWithCounts |> Seq.map snd |> Seq.sum
            
            match listOfChecks.Count with
            | 0 ->
                container.SetListCheckCompleted(true)
                if startingNumberOfChecks > 0 then
                    container.AddNotice($"Pattern monitoring checks completed with {totalPatternsFoundCount} patterns found")
            | _ ->
                container.AddNotice($"Pattern monitoring checks completed, {listOfChecks.Count} remaining")
        }
        
        let monitoringFrequency = TimeSpan.FromMinutes(1)
        
        interface IApplicationService
        
        member _.Execute (cancellationToken:CancellationToken) = task {
            
            try
                do! runThroughMonitoringChecks cancellationToken
            with
                | ex ->
                    logger.LogError("Failed while running alert monitor, will sleep: " + ex.ToString())
                    container.AddNotice("Failed while running alert monitor: " + ex.Message)
                    container.RequestManualRun();
        }
        
        member _.NextRunTime (now:DateTimeOffset) =
            now.Add(monitoringFrequency)