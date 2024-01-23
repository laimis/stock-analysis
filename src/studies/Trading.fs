module studies.Trading

open System
open core.fs.Adapters.Stocks
open studies.Types

let private validateStopLossPercent stopLossPercent =
    match stopLossPercent with
    | Some stopLossPercent when stopLossPercent > 1m -> failwith $"Stop loss percent {stopLossPercent} is greater than 1"
    | Some stopLossPercent when stopLossPercent < 0m -> failwith $"Stop loss percent {stopLossPercent} is less than 0"
    | _ -> ()
        
let private findNextDayBarAndIndex (signal:SignalWithPriceProperties.Row) (prices:PriceBars) =
    match prices.TryFindByDate signal.Date with
    | None -> failwith $"Could not find the price bar for the {signal.Ticker} @ {signal.Date}, unexpected"
    | Some openBarWithIndex ->
        let _, index = openBarWithIndex
        let nextDayIndex = index + 1
        match nextDayIndex >= prices.Length with
        | true -> failwith $"No open day available for {signal.Ticker} @ {signal.Date}"
        | false -> (prices.Bars[nextDayIndex], nextDayIndex)

let strategyWithGenericStopLoss verbose name positionType stopLossReachedFunc (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
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
    
    TradeOutcomeOutput.create name positionType signal openBar closeBar

let strategyWithStopLossPercent verbose positionType numberOfBarsToHold (stopLossPercent:decimal option) (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    validateStopLossPercent stopLossPercent
    
    let stopLossPortion =
        match stopLossPercent with
        | None -> ""
        | Some stopLossPercent -> "SL of " + stopLossPercent.ToString("0.00") + "%"
        
    let holdPeriod =
        match numberOfBarsToHold with
        | None -> ""
        | Some numberOfBarsToHold -> "hold for " + numberOfBarsToHold.ToString() + " bars"
        
    let buyOrSell =
        match positionType with
        | core.fs.Stocks.StockPositionType.Long -> "Buy"
        | core.fs.Stocks.StockPositionType.Short -> "Sell"
        
    let name = String.concat " " ([buyOrSell; holdPeriod; stopLossPortion] |> List.filter (fun x -> x <> ""))
        
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
        | None ->
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> 0m
            | core.fs.Stocks.StockPositionType.Short -> Decimal.MaxValue
        | Some stopLossPercent ->
            let multiplier =
                match positionType with
                | core.fs.Stocks.StockPositionType.Long -> 1m - stopLossPercent
                | core.fs.Stocks.StockPositionType.Short -> 1m + stopLossPercent
            openDay.Open * multiplier
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached =
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> bar.Close < stopPrice
            | core.fs.Stocks.StockPositionType.Short -> bar.Close > stopPrice
            
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    strategyWithGenericStopLoss verbose name positionType stopLossFunc (signal,prices)
    
let strategyWithSignalOpenAsStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    let name = "Buy and use signal open as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Open
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    strategyWithGenericStopLoss verbose name core.fs.Stocks.Long stopLossFunc (signal,prices)
    
let strategyWithSignalCloseAsStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    let name = "Buy and use signal close as stop"
        
    let closeBar = prices.Last        
    let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> fst |> fun (bar:PriceBar) -> bar.Close
    
    let stopLossFunc (index, bar:PriceBar) =
        let stopPriceReached = bar.Close < stopPrice
        let closeDayReached = bar.Date >= closeBar.Date
        (index, bar, stopPriceReached, closeDayReached)
    
    strategyWithGenericStopLoss verbose name core.fs.Stocks.Long stopLossFunc (signal,prices)
    
    
let strategyWithTrailingStop verbose positionType stopLossPercent (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
    
    stopLossPercent |> Some |> validateStopLossPercent
    
    let buyOrSell = 
        match positionType with
        | core.fs.Stocks.StockPositionType.Long -> "Buy"
        | core.fs.Stocks.StockPositionType.Short -> "Sell"
        
    let name = $"{buyOrSell} and use trailing stop, SL of {stopLossPercent}%%"
    
    let stopLossReferencePrice = ref 0m
    
    let closeBar = prices.Last
    
    let stopLossFunc (index, bar:PriceBar) =
        let closeDayReached = bar.Date >= closeBar.Date
        
        if stopLossReferencePrice.Value = 0m then
            stopLossReferencePrice.Value <- bar.Close
        
        let refValue, stopReached =
            match positionType with
            | core.fs.Stocks.StockPositionType.Long ->
                System.Math.Max(bar.Close, stopLossReferencePrice.Value), bar.Close < stopLossReferencePrice.Value * (1m - stopLossPercent)
            | core.fs.Stocks.StockPositionType.Short ->
                System.Math.Min(bar.Close, stopLossReferencePrice.Value), bar.Close > stopLossReferencePrice.Value * (1m + stopLossPercent)
                
        stopLossReferencePrice.Value <- refValue
        
        (index, bar, stopReached, closeDayReached)
    
    strategyWithGenericStopLoss verbose name positionType stopLossFunc (signal,prices)

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
        |> Seq.toList
    
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
        strategies
        |> List.map (fun strategy ->
            signalsWithPriceBars |> List.map strategy
        )
        |> List.concat
        
    return allOutcomes
}