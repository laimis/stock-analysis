namespace core.fs.Alerts

    open System
    open System.Collections.Concurrent
    open core.Account
    open core.Shared
    open core.Stocks.Services.Analysis

    type private StockPositionMonitorKey = {
        Ticker: Ticker
        UserId: Guid
    }
    
    [<Struct>]
    type AlertContainerMessage = {
        message: string
        ``when``: DateTimeOffset
    }
    
    [<Struct>]
    type AlertCheck = {
        ticker: Ticker
        listName: string
        user: UserState
    }
    
    type AlertType =
        | Negative = 0
        | Neutral = 1
        | Positive = 2
        
    module Constants =
        let MonitorTagPattern = "monitor:patterns";
        let MonitorNamePattern = "Patterns";
    
    
    [<Struct>]
    type TriggeredAlert =
        {
            identifier:string
            triggeredValue:decimal
            watchedValue:decimal
            ``when``:DateTimeOffset
            ticker:Ticker
            description:string
            sourceList:string
            userId:Guid
            alertType:AlertType
            valueFormat:ValueFormat
        }
        
        member this.Age() = (DateTimeOffset.UtcNow - this.``when``)
        
        static member GapUpAlert ticker sourceList (gap:Gap) ``when`` userId =
            {
                identifier = "Gap up"
                triggeredValue = gap.gapSizePct
                watchedValue = gap.gapSizePct
                ``when`` = ``when``
                ticker = ticker
                description = "Gap up"
                sourceList = sourceList
                userId = userId
                alertType = AlertType.Positive
                valueFormat = ValueFormat.Percentage 
            }
            
        static member StopPriceAlert ticker price stopPrice ``when`` userId =
            {
                identifier = "Stop price"
                triggeredValue = price
                watchedValue = stopPrice
                ``when`` = ``when``
                ticker = ticker
                description = "Stop price"
                sourceList = "Stop price"
                userId = userId
                alertType = AlertType.Negative
                valueFormat = ValueFormat.Currency
            }
            
        static member PatternAlert (pattern:Pattern) ticker sourceList ``when`` userId =
            {
                identifier = pattern.name
                triggeredValue = pattern.value
                watchedValue = pattern.value
                ``when`` = ``when``
                ticker = ticker
                description = pattern.description
                sourceList = sourceList
                userId = userId
                alertType = AlertType.Neutral
                valueFormat = pattern.valueFormat
            }
    
    type StockAlertContainer() =
        let alerts = ConcurrentDictionary<StockPositionMonitorKey, TriggeredAlert>()
        let recentlyTriggered = ConcurrentDictionary<Guid, List<TriggeredAlert>>()
        let notices = System.Collections.Generic.List<AlertContainerMessage>(capacity=10)
        
        let mutable manualRun = false
        let mutable listChecksCompleted = false
        let mutable stopLossCheckCompleted = false
        
        let addToRecent (alert:TriggeredAlert) =
            
            match recentlyTriggered.TryAdd(key=alert.userId, value=[alert]) with
            | true -> ()
            | false ->
                let list = recentlyTriggered[alert.userId]
                
                let newList =
                    match list.Length with
                    | 20 -> list |> List.tail
                    | _ -> list
                
                recentlyTriggered[alert.userId] <- (newList @ [alert])
        
        member _.RequestManualRun() = manualRun <- true;
        member _.ManualRunRequested() = manualRun
        member _.ManualRunCompleted() = manualRun <- false
        
        
        member _.Register (alert:TriggeredAlert) =
            let key = { Ticker = alert.ticker; UserId = alert.userId }
            
            match alerts.TryAdd(key, alert) with
            | true -> alert |> addToRecent
            | false -> alerts[key] = alert |> ignore
            
        member _.DeregisterAlert (alert:TriggeredAlert) =
            
            { Ticker = alert.ticker; UserId = alert.userId }
            |> alerts.TryRemove |> ignore
            
        member _.Deregister alertIdentifier ticker userId =
            { Ticker = ticker; UserId = userId }
            |> alerts.TryRemove |> ignore
            
        member _.DeregisterStopPriceAlert ticker userId =
            { Ticker = ticker; UserId = userId }
            |> alerts.TryRemove |> ignore
            
        member _.GetRecentlyTriggered (userId:Guid) =
            let list =
                match recentlyTriggered.TryGetValue(userId) with
                | true, alerts -> alerts
                | false, _ -> []
            
            list |> List.sortByDescending (fun x -> x.``when``)
            
        member _.GetAlerts (userId:Guid) =
            alerts.Values
            |> Seq.filter (fun alert -> alert.userId = userId)
            
        member _.AddNotice message =
            if notices.Count = 10 then
                notices.RemoveAt(0)
            
            { message = message; ``when`` = DateTimeOffset.UtcNow }
            |> notices.Add
            
        member _.GetNotices () =
            notices |> Seq.sortByDescending (fun x -> x.``when``)

        member _.SetListCheckCompleted completed = listChecksCompleted <- completed
        member _.SetStopLossCheckCompleted completed = stopLossCheckCompleted <- completed
        member _.ContainerReadyForNotifications() = listChecksCompleted && stopLossCheckCompleted;