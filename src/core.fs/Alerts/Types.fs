namespace core.fs.Alerts

    open System
    open System.Collections.Concurrent
    open core.Account
    open core.Shared
    open core.fs
    open core.fs.Accounts
    open core.fs.Services.Analysis

    type private StockPositionMonitorKey = {
        Ticker: Ticker
        UserId: UserId
        Identifier: string
    }
    
    [<Struct>]
    type AlertContainerMessage = {
        message: string
        ``when``: DateTimeOffset
    }
    
    [<Struct>]
    type PatternCheck = {
        ticker: Ticker
        listName: string
        user: UserState
    }
    
    [<Struct>]
    type StopLossCheck = {
        ticker: Ticker
        stopPrice: decimal
        isShort: bool
        user: UserState
    }
    
    module Constants =
        let MonitorTagPattern = "monitor:patterns"
        let MonitorNamePattern = "Patterns"
        let StopLossIdentifier = "Stop loss"
        let PortfolioIdentifier = "💼 Portfolio"
        let PendingIdentifier = "⏳ Pending"
    
    
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
            userId:UserId
            alertType:SentimentType
            valueFormat:ValueFormat
        }
        
        member this.Age() = (DateTimeOffset.UtcNow - this.``when``)
            
        static member StopPriceAlert ticker price stopPrice ``when`` userId =
            {
                identifier = Constants.StopLossIdentifier
                triggeredValue = price
                watchedValue = stopPrice
                ``when`` = ``when``
                ticker = ticker
                description = "Stop price"
                sourceList = "Stop price"
                userId = userId
                alertType = SentimentType.Negative
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
                alertType = pattern.sentimentType
                valueFormat = pattern.valueFormat
            }
        
    type StockAlertContainer() =
        let alerts = ConcurrentDictionary<StockPositionMonitorKey, TriggeredAlert>()
        let recentlyTriggered = ConcurrentDictionary<UserId, List<TriggeredAlert>>()
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
            let key = { Ticker = alert.ticker; UserId = alert.userId; Identifier = alert.identifier  }
            
            match alerts.TryAdd(key, alert) with
            | true -> alert |> addToRecent
            | false -> alerts[key] = alert |> ignore
            
        member _.DeregisterAlert (alert:TriggeredAlert) =
            
            { Ticker = alert.ticker; UserId = alert.userId; Identifier = alert.identifier }
            |> alerts.TryRemove |> ignore
            
        member _.Deregister ticker identifier userId =
            { Ticker = ticker; UserId = userId; Identifier = identifier }
            |> alerts.TryRemove |> ignore
            
        member _.GetRecentlyTriggered userId =
            let list =
                match recentlyTriggered.TryGetValue(userId) with
                | true, alerts -> alerts
                | false, _ -> []
            
            list |> List.sortByDescending (fun x -> x.``when``)
            
        member _.GetAlerts userId =
            alerts.Values
            |> Seq.filter (fun alert -> alert.userId = userId)
            |> Seq.sortBy (fun x -> x.sourceList,x.ticker.Value)
            
        member _.AddNotice message =
            if notices.Count = 10 then
                notices.RemoveAt(0)
            
            { message = message; ``when`` = DateTimeOffset.UtcNow }
            |> notices.Add
            
        member _.GetNotices () =
            notices |> Seq.sortByDescending (fun x -> x.``when``)

        member _.SetListCheckCompleted completed = listChecksCompleted <- completed
        member _.SetStopLossCheckCompleted completed = stopLossCheckCompleted <- completed
        member _.ContainerReadyForNotifications() = listChecksCompleted && stopLossCheckCompleted
        member this.ClearStopLossAlert() =
            alerts.Values
            |> Seq.filter (fun x -> x.identifier = Constants.StopLossIdentifier)
            |> Seq.map (fun x -> { Ticker = x.ticker; UserId = x.userId; Identifier = Constants.StopLossIdentifier })
            |> Seq.iter (fun x -> alerts.TryRemove(x) |> ignore)
            