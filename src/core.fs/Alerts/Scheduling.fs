module core.fs.Alerts.Scheduling

    open System
    open core.Shared.Adapters.Brokerage
    
    let private _monitorTimes = [
        TimeOnly.Parse("09:45")
        TimeOnly.Parse("11:15")
        TimeOnly.Parse("13:05")
        TimeOnly.Parse("14:35")
        TimeOnly.Parse("15:30")
    ]

    let nextRun referenceTimeUtc (marketHours:IMarketHours) =
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

