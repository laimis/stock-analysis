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
    open core.fs.Adapters.Storage
    open core.fs.Stocks
    
    // stop loss should be monitored at the following times:
    // on trading days every 5 minutes from 9:45am to 4:00pm
    // and no monitoring on weekends
    let private marketStartTime = TimeOnly(9, 30, 0)
    let private marketEndTime = TimeOnly(16, 0, 0)
    
        
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
        
        let runStopLossCheck (user:UserState) (_:CancellationToken) (position:StockPositionState) = async {
            let! priceResponse = brokerage.GetQuote user position.Ticker |> Async.AwaitTask
            
            match priceResponse.Success with
            | None -> logError($"Could not get price for {position.Ticker}: {priceResponse.Error.Value.Message}")
            | Some response ->
                
                let pctToStop = position |> StockPositionWithCalculations |> fun x -> x.PercentToStop response.Price
                match pctToStop >= 0m with
                | true ->
                    TriggeredAlert.StopPriceAlert position.Ticker response.Price position.StopPrice.Value DateTimeOffset.UtcNow (user.Id |> UserId)
                    |> container.Register
                | false ->
                    container.DeregisterStopPriceAlert position.Ticker (user.Id |> UserId)
        }
        
        let runStopLossCheckForUser (cancellationToken:CancellationToken) userId = task {
            
            match cancellationToken.IsCancellationRequested with
            | true ->
                ()
            | false ->
                logInformation($"Running stop loss check for {userId}")
                
                let! user = accounts.GetUser(userId) |> Async.AwaitTask
                
                match user with
                | None -> logError $"Unable to find user {userId}"
                | Some user ->
                        logInformation $"Found user {userId}"
                        
                        let! checks = user.Id |> UserId |> portfolio.GetStockPositions
                        
                        let! _ =
                            checks
                            |> Seq.filter (fun s -> s.IsOpen && s.HasStopPrice)
                            |> Seq.map (fun p -> p |> runStopLossCheck user.State cancellationToken)
                            |> Async.Sequential // <- don't do this in parallel, usually ends up overloading the brokerage
                            |> Async.StartAsTask
                            
                        logInformation("done")
        }
            
        interface IApplicationService
        
        member _.Execute cancellationToken = task {
            
            container.AddNotice("Running stop loss checks")
            container.SetStopLossCheckCompleted(false)
            container.ClearStopLossAlert()
            
            let! users = accounts.GetUserEmailIdPairs()
            
            let! _ =
                users
                |> Seq.map (fun emailIdPair ->
                    runStopLossCheckForUser cancellationToken emailIdPair.Id
                    |> Async.AwaitTask)
                |> Async.Sequential
                |> Async.StartAsTask
                
            container.SetStopLossCheckCompleted(true)
            container.AddNotice("Stop loss checks completed")
        }
        
        member _.NextRunTime now =
            
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
        let listChecks = Dictionary<string, List<AlertCheck>>()
        let priceCache = Dictionary<Ticker, PriceBars>()
        
        let generateAlertListForUser (emailIdPair:EmailIdPair) = async {
            let! user = emailIdPair.Id |> accounts.GetUser |> Async.AwaitTask
                        
            match user with
            | None ->
                return Seq.empty<AlertCheck>
            | Some user ->
                let! lists = emailIdPair.Id |> portfolio.GetStockLists |> Async.AwaitTask
                
                let! ownedStocks = emailIdPair.Id |> portfolio.GetStockPositions |> Async.AwaitTask
                
                let portfolioList =
                    ownedStocks
                    |> Seq.filter (_.IsOpen)
                    |> Seq.map (_.Ticker)
                    |> Seq.map (fun t -> {ticker=t; listName="Portfolio"; user=user.State})
                
                return lists
                |> Seq.filter (fun l -> l.State.ContainsTag(Constants.MonitorTagPattern))
                |> Seq.map (fun l -> l.State.Tickers |> Seq.map (fun t -> {ticker=t.Ticker; listName=l.State.Name; user=user.State}))
                |> Seq.concat
                |> Seq.append portfolioList
        }
        
        let generatePatternMonitoringChecks() = task {
            listChecks.Clear()
            
            let! users = accounts.GetUserEmailIdPairs()
            
            let alertList = List<AlertCheck>()
            
            let! alertListOps =
                users
                |> Seq.map generateAlertListForUser
                |> Async.Sequential
                |> Async.StartAsTask
                
            let allAlerts = alertListOps |> Seq.concat
            
            alertList.AddRange allAlerts
            
            listChecks.Add(Constants.MonitorTagPattern, alertList)
                
            nextPatternMonitoringRunDateTime <- nextPatternMonitoringRun DateTimeOffset.UtcNow marketHours
            
            container.ManualRunCompleted()
            
            let description = String.Join(", ", listChecks |> Seq.map (fun kp -> $"{kp.Key} {kp.Value.Count} checks"))
            
            container.AddNotice(
                 $"Alert check generator added {description}, next run at {marketHours.ToMarketTime(nextPatternMonitoringRunDateTime)}"
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
        
        let runCheck alertCheck = async {
            
            let! priceResponse = getPrices alertCheck.user alertCheck.ticker |> Async.AwaitTask
            
            match priceResponse.Success with
            | None ->
                logger.LogError($"Could not get price for {alertCheck.ticker}: {priceResponse.Error.Value.Message}")
                return None
            | Some prices ->
                
                let userId = alertCheck.user.Id |> UserId
                    
                PatternDetection.availablePatterns
                |> Seq.iter( fun pattern ->
                    container.Deregister pattern alertCheck.ticker userId
                )
                
                let patterns = PatternDetection.generate prices
                
                patterns |> List.iter (fun p ->
                    let alert = TriggeredAlert.PatternAlert p alertCheck.ticker alertCheck.listName DateTimeOffset.UtcNow userId
                    container.Register alert
                )
                
                logger.LogInformation($"Found {patterns.Length} patterns for {alertCheck.ticker}")
                
                return Some alertCheck
        }
        
        let runThroughMonitoringChecks (cancellationToken:CancellationToken) = task {
            let now = DateTimeOffset.UtcNow
            
            if now > nextPatternMonitoringRunDateTime || container.ManualRunRequested() then
                container.AddNotice("Running pattern monitoring checks")
                container.SetListCheckCompleted(false)
                do! generatePatternMonitoringChecks()
                
            priceCache.Clear()
            
            let! _ =
                listChecks
                |> Seq.where (fun kp -> kp.Value.Count > 0)
                |> Seq.map (fun kp -> async {
                        
                        let! completedChecks =
                            kp.Value
                            |> Seq.takeWhile (fun _ -> cancellationToken.IsCancellationRequested |> not)
                            |> Seq.map runCheck
                            |> Async.Sequential
                            
                        completedChecks |> Seq.choose id |> Seq.iter (fun c -> kp.Value.Remove(c) |> ignore)
                    }
                )
                |> Async.Sequential
                |> Async.StartAsTask
                
            if listChecks |> Seq.forall (fun kp -> kp.Value.Count = 0) then
                container.AddNotice("Pattern monitoring checks completed")
                container.SetListCheckCompleted(true)
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