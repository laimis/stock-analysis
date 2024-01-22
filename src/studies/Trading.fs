module studies.Trading

open core.fs.Adapters.Stocks
open studies.Types

let private findNextDayBarAndIndex (signal:SignalWithPriceProperties.Row) (prices:PriceBars) =
    match prices.TryFindByDate signal.Date with
    | None -> failwith $"Could not find the price bar for the {signal.Ticker} @ {signal.Date}, unexpected"
    | Some openBarWithIndex ->
        let _, index = openBarWithIndex
        let nextDayIndex = index + 1
        match nextDayIndex >= prices.Length with
        | true -> failwith $"No open day available for {signal.Ticker} @ {signal.Date}"
        | false -> (prices.Bars[nextDayIndex], nextDayIndex)

let buyAndHoldStrategyWithGenericStopLoss verbose name stopLossReachedFunc (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
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
        | Some (_, closeBar:PriceBar, stopLossReached, _) ->
            let reason = if stopLossReached then "stop loss reached" else "close day reached"
            if verbose then printfn $"Close bar for %s{signal.Ticker} on %A{signal.Date} is %A{closeBar.Date} because {reason}"
            closeBar
    
    TradeOutcomeOutput.create name signal openBar closeBar

let buyAndHoldStrategyWithStopLossPercent verbose numberOfBarsToHold (stopLossPercent:decimal option) (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
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
        | Some stopLossPercent -> openDay.Open * (1m - stopLossPercent)
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
let buyAndHoldWithSignalOpenAsStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    let name = "B&H with signal open as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Open
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
let buyAndHoldWithSignalCloseAsStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    let name = "B&H with signal close as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Close
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)
    
    
let buyAndHoldWithTrailingStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    let name = "B&H with trailing stop"
    let maxPriceSeen = ref 0m
    
    let closeBar = prices.Last
    
    let stopLossFunc (index, bar:PriceBar) =
        let closeDayReached = bar.Date >= closeBar.Date
        
        if bar.Close > maxPriceSeen.Value then maxPriceSeen.Value <- bar.Close
        
        let stopPriceReached = bar.Close < maxPriceSeen.Value * 0.95m
        
        (index, bar, stopPriceReached, closeDayReached)
    
    buyAndHoldStrategyWithGenericStopLoss verbose name stopLossFunc (signal,prices)

let private prepareSignalsForTradeSimulations (priceFunc:string -> Async<PriceBars>) signals = async {
    
    signals |> Seq.map Output |> Unified.describeRecords
    
    // ridiculous, sometimes data provider does not have prices for the date
    // so we filter those records out
    let! asyncData =
        signals
        |> Seq.map (fun r -> async {
            let! prices = r.Ticker |> priceFunc
            let startBar = r.Date |> prices.TryFindByDate
            return (r, prices, startBar)   
        })
        |> Async.Sequential
        
    let signalsWithPriceBars =
        asyncData
        |> Seq.choose (fun (r,prices,startBar) ->
            match startBar with
            | None -> None
            | Some _ -> Some (r, prices)
        )
    
    printfn "Ensured that data has prices"
    
    signalsWithPriceBars |> Seq.map fst |> Seq.map Output |> Unified.describeRecords
    
    return signalsWithPriceBars
}
    
let runTrades getPricesFunc signals strategies = async {
    
    let! signalsWithPriceBars =
        signals
        |> prepareSignalsForTradeSimulations getPricesFunc
    
    printfn "Executing trades..."
    
    let allOutcomes =
        signalsWithPriceBars
        |> Seq.map (fun signalWithPriceBars ->
            strategies |> Seq.map (fun strategy ->
                strategy signalWithPriceBars
            )
        )
        |> Seq.concat
        |> Seq.toList
        
    return allOutcomes
}