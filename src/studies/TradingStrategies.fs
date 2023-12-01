module studies.TradingStrategies

open core.fs.Shared.Adapters.Stocks
open studies.Types

let buyAndHoldStrategy numberOfBarsToHold (signal:GapStudyOutput.Row,prices:PriceBars) =
    // we will buy this stock at the open price of the next day
    let name =
        match numberOfBarsToHold with
        | None -> "B&H"
        | Some numberOfDaysToHold -> $"B&H {numberOfDaysToHold} bars"
        
    // find the next day
    let openDay, openDayIndex =
        match signal.Date.AddDays(1) |> prices.TryFindByDate with
        | None -> signal.Date |> prices.TryFindByDate |> Option.get // if we can't find the next day, then just open at the signal date
        | Some openDay -> openDay
    
    // find the close day
    let closeBar =
        match numberOfBarsToHold with
        | None -> prices.Last
        | Some numberOfDaysToHold ->
            let closeDayIndex = openDayIndex + numberOfDaysToHold
            match closeDayIndex with
            | x when x >= prices.Length -> prices.Last
            | _ -> prices.Bars[closeDayIndex]
    
    let openPrice = openDay.Open
    let closePrice = closeBar.Close
    
    // calculate gain percentage
    let gain = (closePrice - openPrice) / openPrice * 100m
    
    let daysHeld = closeBar.Date - openDay.Date
    
    TradeOutcomeOutput.Row(
        strategy=name,
        ticker=signal.Ticker,
        date=signal.Date,
        screenerid=signal.Screenerid,
        hasGapUp=signal.HasGapUp,
        opened=openDay.Date.DateTime,
        openPrice=openPrice,
        closed=closeBar.Date.DateTime,
        closePrice=closePrice,
        percentGain=gain,
        numberOfDaysHeld=(daysHeld.TotalDays |> int)
    )
    
let buyAndHoldStrategyWithStopLoss verbose numberOfBarsToHold (stopLossPercent:decimal) (signal:GapStudyOutput.Row,prices:PriceBars) =
    // we will buy this stock at the open price of the next day
    let name =
        match numberOfBarsToHold with
        | None -> "B&H with Stop Loss of " + stopLossPercent.ToString("0.00") + "%"
        | Some numberOfDaysToHold -> $"B&H {numberOfDaysToHold} bars with Stop Loss of " + stopLossPercent.ToString("0.00") + "%"
        
    // find the next day
    let openDay, openDayIndex =
        match signal.Date.AddDays(1) |> prices.TryFindByDate with
        | None -> signal.Date |> prices.TryFindByDate |> Option.get // if we can't find the next day, then just open at the signal date
        | Some openDay -> openDay
        
    if verbose then printfn $"Open day for %s{signal.Ticker} on %A{signal.Date} is %A{openDay.Date} with open day index of %A{openDayIndex}"
    
    let closeDayIndex =
        match numberOfBarsToHold with
        | None -> prices.Length - 1
        | Some numberOfBarsToHold ->
            let closeDayIndex = openDayIndex + numberOfBarsToHold
            match closeDayIndex with
            | x when x >= prices.Length -> prices.Length - 1
            | _ -> closeDayIndex
            
    if verbose then printfn $"Close day for %s{signal.Ticker} on %A{signal.Date} is based on %A{numberOfBarsToHold} number of bars to hold and is %A{prices.Bars[closeDayIndex].Date} with close day index of %A{closeDayIndex}"
            
    // find the close day
    let closeBar =
        prices.Bars
        |> Seq.indexed
        |> Seq.skip openDayIndex
        |> Seq.map (fun (index, bar) ->
            let stopLossReached = (bar.Close - openDay.Open) / openDay.Open * 100m < -stopLossPercent
            let closeDayReached = index = closeDayIndex
            (index, bar, stopLossReached, closeDayReached)
        )
        |> Seq.filter (fun (_, _, stopLossReached, closeDayReached) -> stopLossReached || closeDayReached)
        |> Seq.tryHead
        
    let closeBar =
        match closeBar with
        | None ->
            // TODO: investigate how this could happen
            if verbose then printfn $"Could not find close bar for %s{signal.Ticker} on %A{signal.Date}"
            prices.Bars[closeDayIndex]
        | Some (index, bar, stopLossReached, closeDayReached) ->
            let reason = if stopLossReached then "stop loss reached" else "close day reached"
            if verbose then printfn $"Close bar for %s{signal.Ticker} on %A{signal.Date} is %A{bar.Date} because %s{reason}"
            bar
    
    let openPrice = openDay.Open
    let closePrice = closeBar.Close
    
    // calculate gain percentage
    let gain = (closePrice - openPrice) / openPrice * 100m
    
    let daysHeld = closeBar.Date - openDay.Date
    
    TradeOutcomeOutput.Row(
        strategy=name,
        ticker=signal.Ticker,
        date=signal.Date,
        screenerid=signal.Screenerid,
        hasGapUp=signal.HasGapUp,
        opened=openDay.Date.DateTime,
        openPrice=openPrice,
        closed=closeBar.Date.DateTime,
        closePrice=closePrice,
        percentGain=gain,
        numberOfDaysHeld=(daysHeld.TotalDays |> int)
    )