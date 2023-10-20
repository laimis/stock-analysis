module core.fs.Alerts.MonitoringServices

    open System
    open System.Threading
    open core.Account
    open core.Stocks
    open core.fs.Shared
    open core.fs.Shared.Adapters.Brokerage
    open core.fs.Shared.Adapters.Logging
    open core.fs.Shared.Adapters.Storage
    open core.fs.Shared.Domain.Accounts

    // stop loss should be monitored at the following times:
    // on trading days every 5 minutes from 9:45am to 4:00pm
    // and no monitoring on weekends
    let private marketStartTime = TimeOnly(9, 30, 0);
    let private marketEndTime = TimeOnly(16, 0, 0)
    let nextStopLossRun now (marketHours:IMarketHours) =
        let eastern = marketHours.ToMarketTime(now)
        let marketStartTimeInEastern = DateTimeOffset(eastern.Date.Add(marketStartTime.ToTimeSpan()))
        
        let nextScan =
            match TimeOnly.FromTimeSpan(eastern.TimeOfDay) with
            | t when t < marketStartTime -> marketStartTimeInEastern
            | t when t > marketEndTime -> marketStartTimeInEastern.AddDays(1).AddMinutes(15)
            | _ -> eastern.AddMinutes(5)
            
        let adjustedScanTime =
            match nextScan.DayOfWeek with
            | DayOfWeek.Saturday -> nextScan.AddDays(2)
            | DayOfWeek.Sunday -> nextScan.AddDays(1)
            | _ -> nextScan

        marketHours.ToUniversalTime(adjustedScanTime)
        
    let private _monitorTimes = [
        TimeOnly.Parse("09:45")
        TimeOnly.Parse("11:15")
        TimeOnly.Parse("13:05")
        TimeOnly.Parse("14:35")
        TimeOnly.Parse("15:30")
    ]
    let nextMonitoringRun referenceTimeUtc (marketHours:IMarketHours) =
        let easternTime = marketHours.ToMarketTime(referenceTimeUtc)
        
        let candidates =
            _monitorTimes
            |> List.map (fun t -> DateTimeOffset(easternTime.Date.Add(t.ToTimeSpan())))
            
            
        let candidatesInFuture =
            candidates
            |> List.filter (fun t -> t > easternTime)
            |> List.map (fun t -> marketHours.ToUniversalTime(t))
            
        match candidatesInFuture with
        | head :: _ -> head
        | _ -> 
            // if we get here, we need to look at the next day
            let nextDay =
                match candidates.Head.AddDays(1).DayOfWeek with
                | DayOfWeek.Saturday -> candidates.Head.AddDays(3)
                | DayOfWeek.Sunday -> candidates.Head.AddDays(2)
                | _ -> candidates.Head.AddDays(1)
            
            marketHours.ToUniversalTime(nextDay);
        
    type StopLossMonitoringService(accounts:IAccountStorage, brokerage:IBrokerage, container:StockAlertContainer, portfolio:IPortfolioStorage, logger:ILogger) =
        
        // need to decide how I will log these
        let logInformation = logger.LogInformation
        let logError = logger.LogError
        
        let runStopLossCheck (user:UserState) (_:CancellationToken) (position:PositionInstance) = async {
            let! priceResponse = brokerage.GetQuote user position.Ticker |> Async.AwaitTask
            
            match priceResponse.Success with
            | None -> logError($"Could not get price for {position.Ticker}: {priceResponse.Error.Value.Message}")
            | Some response ->
                
                let price = response.Price
                match price <= position.StopPrice.Value with
                | true ->
                    TriggeredAlert.StopPriceAlert position.Ticker price position.StopPrice.Value DateTimeOffset.UtcNow (user.Id |> UserId)
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
                        
                        let! checks = user.Id |> UserId |> portfolio.GetStocks
                        
                        let! _ =
                            checks
                            |> Seq.filter (fun s -> s.State.OpenPosition <> null)
                            |> Seq.map (fun s -> s.State.OpenPosition)
                            |> Seq.filter (fun p -> p.StopPrice.HasValue)
                            |> Seq.map (fun p -> p |> runStopLossCheck user.State cancellationToken)
                            |> Async.Parallel
                            |> Async.StartAsTask
                            
                        logInformation("done")
        }
            
        interface IApplicationService
        
        member _.Execute cancellationToken = task {
            
            container.SetStopLossCheckCompleted(false)
            
            let! users = accounts.GetUserEmailIdPairs()
            
            let! _ =
                users
                |> Seq.map (fun emailIdPair ->
                    runStopLossCheckForUser cancellationToken emailIdPair.Id
                    |> Async.AwaitTask)
                |> Async.Sequential
                |> Async.StartAsTask
                
            container.SetStopLossCheckCompleted(true)
        }