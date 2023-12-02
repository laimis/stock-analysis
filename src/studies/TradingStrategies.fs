module studies.TradingStrategies

open core.fs.Shared.Adapters.Stocks
open studies.Types


let private toOutputRow name (signal:GapStudyOutput.Row) (openBar:PriceBar) (closeBar:PriceBar) =
    
    let openPrice = openBar.Open
    let closePrice = closeBar.Close
    
    // calculate gain percentage
    let gain = (closePrice - openPrice) / openPrice
    
    let daysHeld = closeBar.Date - openBar.Date
    
    TradeOutcomeOutput.Row(
        strategy=name,
        ticker=signal.Ticker,
        date=signal.Date,
        screenerid=signal.Screenerid,
        hasGapUp=signal.HasGapUp,
        opened=openBar.Date.DateTime,
        openPrice=openPrice,
        closed=closeBar.Date.DateTime,
        closePrice=closePrice,
        percentGain=gain,
        numberOfDaysHeld=(daysHeld.TotalDays |> int)
    )
    
let private findNextDayBarAndIndex (signal:GapStudyOutput.Row) (prices:PriceBars) =
    match prices.TryFindByDate signal.Date with
    | None -> failwith $"Could not find the price bar for the {signal.Ticker} @ {signal.Date}, unexpected"
    | Some openBarWithIndex ->
        let _, index = openBarWithIndex
        let nextDayIndex = index + 1
        match nextDayIndex >= prices.Length with
        | true -> failwith $"No open day available for {signal.Ticker} @ {signal.Date}"
        | false -> (prices.Bars[nextDayIndex], nextDayIndex)

let buyAndHoldStrategyWithGenericStopLoss verbose name stopLossReachedFunc (signal:GapStudyOutput.Row,prices:PriceBars) =
    let openBar, openDayIndex = findNextDayBarAndIndex signal prices
    
    if verbose then printfn $"Open bar for %s{signal.Ticker} on %A{signal.Date} is %A{openBar.Date} @ %A{openBar.Open}"
            
    // find the close day
    let closeBar =
        prices.Bars
        |> Seq.indexed
        |> Seq.skip openDayIndex
        |> Seq.map stopLossReachedFunc
        |> Seq.filter (fun (_, _, stopLossReached, closeDayReached) -> stopLossReached || closeDayReached)
        |> Seq.tryHead
        
    let closeBar =
        match closeBar with
        | None ->
            //if verbose then printfn $"Could not find close bar for %s{signal.Ticker} on %A{signal.Date}"
            failwith $"Could not find close bar for %s{signal.Ticker} on %A{signal.Date}"
        | Some (_, (closeBar:PriceBar), stopLossReached, _) ->
            let reason = if stopLossReached then "stop loss reached" else "close day reached"
            if verbose then printfn $"Close bar for %s{signal.Ticker} on %A{signal.Date} is %A{closeBar.Date} because {reason}"
            closeBar
    
    toOutputRow name signal openBar closeBar

let buyAndHoldStrategyWithStopLossPercent verbose numberOfBarsToHold (stopLossPercent:decimal option) (signal:GapStudyOutput.Row,prices:PriceBars) =
    
    let stopLossPortion =
        match stopLossPercent with
        | None -> ""
        | Some stopLossPercent -> "SL of " + stopLossPercent.ToString("0.00") + "%"
        
    let holdPeriod =
        match numberOfBarsToHold with
        | None -> ""
        | Some numberOfBarsToHold -> numberOfBarsToHold.ToString() + " bars"
        
    let name = String.concat " " (["B&H"; holdPeriod; stopLossPortion] |> List.filter (fun x -> x <> ""))
        
    let openDay, openDayIndex = findNextDayBarAndIndex signal prices
    
    let closeBar =
        match numberOfBarsToHold with
        | None -> prices.Last
        | Some numberOfBarsToHold ->
            let closeDayIndex = openDayIndex + numberOfBarsToHold
            match closeDayIndex with
            | x when x >= prices.Length -> prices.Last
            | _ -> prices.Bars[closeDayIndex]
            
    let stopPrice =
        match stopLossPercent with
        | None -> 0m
        | Some stopLossPercent -> openDay.Open * (1m - stopLossPercent / 100m)
    
    let stopLossFunc (index, (bar:PriceBar)) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
let buyAndHoldWithSignalOpenAsStop verbose (signal:GapStudyOutput.Row,prices:PriceBars) =
    
    let name = "B&H with signal open as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Open
    
    let stopLossFunc (index, (bar:PriceBar)) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
let buyAndHoldWithSignalCloseAsStop verbose (signal:GapStudyOutput.Row,prices:PriceBars) =
    
    let name = "B&H with signal close as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Close
    
    let stopLossFunc (index, (bar:PriceBar)) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
    
let buyAndHoldWithTrailingStop verbose (signal:GapStudyOutput.Row,prices:PriceBars) =
    
    let name = "B&H with signal with trailing stop"
    let maxPriceSeen = ref 0m
    
    let closeBar = prices.Last
    
    let stopLossFunc (index, (bar:PriceBar)) =
        let closeDayReached = bar.Date >= closeBar.Date
        
        if bar.Close > maxPriceSeen.Value then maxPriceSeen.Value <- bar.Close
        
        let stopPriceReached = bar.Close < maxPriceSeen.Value * 0.95m
        
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    