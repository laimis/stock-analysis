namespace core.fs.Alerts

    open System
    open System.Collections.Concurrent
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
    
    module Constants =
        let MonitorTagPattern = "monitor:patterns"
        let MonitorTagObvPriceTrend = "monitor:obvpricetrend"
        let MonitorNamePattern = "Patterns"
        let MonitorNameObvPriceTrend = "OBV Price Trend"
        let StopLossIdentifier = "Stop loss"
        let StockPortfolioIdentifier = "💼 - Stocks"
        let OptionPortfolioIdentifier = "💼 - Options"
        let StocksPendingIdentifier = "⏳ - Stocks"
        let OptionsPendingIdentifier = "⏳ - Options"
    
    
    [<Struct>]
    type TriggeredAlert =
        {
            identifier:string
            triggeredValue:decimal
            watchedValue:decimal
            ``when``:DateTimeOffset
            ticker:Ticker
            description:string
            sourceLists:string list
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
                sourceLists = [Constants.StockPortfolioIdentifier]
                userId = userId
                alertType = SentimentType.Negative
                valueFormat = ValueFormat.Currency
            }
            
        static member PatternAlert (pattern:Pattern) ticker sourceLists ``when`` userId =
            {
                identifier = pattern.name
                triggeredValue = pattern.value
                watchedValue = pattern.value
                ``when`` = ``when``
                ticker = ticker
                description = pattern.description
                sourceLists = sourceLists
                userId = userId
                alertType = pattern.sentimentType
                valueFormat = pattern.valueFormat
            }
        
    type AlertsView = {
        alerts: TriggeredAlert list
        recentlyTriggered: TriggeredAlert list
        messages: AlertContainerMessage seq
    }
        
    type StockAlertContainer() =
        let alerts = ConcurrentDictionary<StockPositionMonitorKey, TriggeredAlert>()
        let recentlyTriggered = ConcurrentDictionary<UserId, List<TriggeredAlert>>()
        let notices = System.Collections.Generic.List<AlertContainerMessage>(capacity=10)
        
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
                
        let clearAlerts condition =
            alerts.Values
            |> Seq.filter condition
            |> Seq.map (fun x -> { Ticker = x.ticker; UserId = x.userId; Identifier = x.identifier })
            |> Seq.iter (fun x -> alerts.TryRemove(x) |> ignore)
        
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
            |> Seq.sortBy (fun x -> x.sourceLists.Head,x.ticker.Value)
            
        member _.AddNotice message =
            if notices.Count = 10 then
                notices.RemoveAt(0)
            
            { message = message; ``when`` = DateTimeOffset.UtcNow }
            |> notices.Add
            
        member _.GetNotices () =
            notices |> Seq.sortByDescending (_.``when``)

        member this.ClearStopLossAlert() =
            clearAlerts (fun x -> x.identifier = Constants.StopLossIdentifier)
            
        member this.ClearNonStopLossAlerts() =
            clearAlerts (fun x -> x.identifier <> Constants.StopLossIdentifier)
