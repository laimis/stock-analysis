module studies.ScreenerStudy

open System
open System.Collections.Generic
open FSharp.Data
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis
open studies.DataHelpers
open Microsoft.Extensions.Logging
open studies.ServiceHelper

module Constants =
    [<Literal>]
    let MinimumRecords = 1_000

    [<Literal>]
    let EarliestYear = 2020

    [<Literal>]
    let LatestYear = 2024

type Signal =
    CsvProvider<
        Sample = "screenerid (int), date (string), ticker (string)",
        HasHeaders=true
    >

type SignalWithPriceProperties =
    CsvProvider<
        Schema = "screenerid (int), date (string), ticker (string), gap (decimal), sma20 (decimal), sma50 (decimal), sma150 (decimal), sma200 (decimal)",
        HasHeaders=false
    >

type TradeOutcomeOutput =
    CsvProvider<
        Sample = "screenerid (int), date (string), ticker (string), gap (decimal), sma20 (decimal), sma50 (decimal), sma150 (decimal), sma200 (decimal), strategy (string), longOrShort (string), opened (string), openPrice (decimal), closed (string), closePrice (decimal), percentGain (decimal), numberOfDaysHeld (int)",
        HasHeaders=true
    >

module Signal =
    let verifyRecords (input:Signal) minimumRecordsExpected =
        let records = input.Rows
        // make sure there is at least some records in here, ideally in thousands
        let numberOfRecords = records |> Seq.length
        match numberOfRecords with
        | x when x < minimumRecordsExpected -> failwith $"{x} is not enough records, expecting at least {minimumRecordsExpected}"
        | _ -> ()

        let verifyCondition failureConditionFunc messageIfFound =
            records
            |> Seq.map (fun r -> match failureConditionFunc r with | true -> Some r | false -> None)
            |> Seq.choose id
            |> Seq.tryHead
            |> Option.iter (fun r -> r |> messageIfFound |> failwith)

        // make sure all the dates are set (and can be parsed?)
        let invalidDate = fun (r:Signal.Row) -> match DateTimeOffset.TryParse(r.Date) with | true, _ -> false | false, _ -> true
        let messageIfInvalidDate = fun (r:Signal.Row) -> $"date is invalid for record {r.Screenerid}, {r.Ticker}"
        verifyCondition invalidDate messageIfInvalidDate

        let invalidTicker = fun (r:Signal.Row) -> String.IsNullOrWhiteSpace(r.Ticker)
        let messageIfInvalidTicker = fun (r:Signal.Row) -> $"ticker is blank for record {r.Screenerid}, {r.Date}"
        verifyCondition invalidTicker messageIfInvalidTicker

        let invalidScreenerId = fun (r:Signal.Row) -> r.Screenerid = 0
        let messageIfInvalidScreenerId = fun (r:Signal.Row) -> $"screenerid is blank for record {r.Ticker}, {r.Date}"
        verifyCondition invalidScreenerId messageIfInvalidScreenerId

        records

type Unified =
    | Input of Signal.Row
    | Output of SignalWithPriceProperties.Row

module Unified =
    let describeRecords (records:Unified seq) =

        let getDate unified =
            match unified with
            | Input row -> row.Date
            | Output row -> row.Date

        let getTicker unified =
            match unified with
            | Input row -> row.Ticker
            | Output row -> row.Ticker

        let getScreenerId unified =
            match unified with
            | Input row -> row.Screenerid
            | Output row -> row.Screenerid

        let numberOfRecords = records |> Seq.length

        match numberOfRecords with
        | x when x > 0 ->
            let dates = records |> Seq.map (fun r -> r |> getDate) |> Seq.distinct |> Seq.length
            let tickers = records |> Seq.map (fun r -> r |> getTicker) |> Seq.distinct |> Seq.length
            let screenerIds = records |> Seq.map (fun r -> r |> getScreenerId) |> Seq.distinct |> Seq.length

            let minimumDate = records |> Seq.minBy (fun r -> r |> getDate) |> getDate
            let maximumDate = records |> Seq.maxBy (fun r -> r |> getDate) |> getDate

            printfn $"Records: %d{numberOfRecords}, dates: %d{dates}, tickers: %d{tickers}, screenerIds: %d{screenerIds}"
            printfn $"Minimum date: %A{minimumDate}"
            printfn $"Maximum date: %A{maximumDate}"
            printfn ""
        | _ ->
            printfn $"No records found in the input"

module TradeOutcomeOutput =

    let create strategy positionType (signal:SignalWithPriceProperties.Row) (openBar:PriceBar) (closeBar:PriceBar) =

        let openPrice = openBar.Open
        let closePrice = closeBar.Close

        let daysHeld = closeBar.Date - openBar.Date

        let longOrShortString, gainFactor =
            match positionType with
            | core.fs.Stocks.StockPositionType.Long -> "long", 1m
            | core.fs.Stocks.StockPositionType.Short -> "short", -1m

        // calculate gain percentage
        let gain = (closePrice - openPrice) / openPrice * gainFactor

        TradeOutcomeOutput.Row(
            screenerid=signal.Screenerid,
            date=signal.Date,
            ticker=signal.Ticker,
            gap=signal.Gap,
            sma20=signal.Sma20,
            sma50=signal.Sma50,
            sma150=signal.Sma150,
            sma200=signal.Sma200,
            strategy=strategy,
            longOrShort=longOrShortString,
            opened=openBar.DateStr,
            openPrice=openPrice,
            closed=closeBar.DateStr,
            closePrice=closePrice,
            percentGain=gain,
            numberOfDaysHeld=(daysHeld.TotalDays |> int)
        )

type TradeSummary = {
    StrategyName:string
    NumberOfTrades:int
    Winners:int
    Losers:int
    TotalGain:decimal
    WinPct:decimal
    AvgWin:decimal
    AvgLoss:decimal
    AvgGainLoss:decimal
    EV:decimal
    Gains:decimal seq
    GainDistribution:core.fs.Services.Analysis.DistributionStatistics
}

module TradeSummary =
    let create name (outcomes:TradeOutcomeOutput.Row seq) =
        // summarize the outcomes
        let numberOfTrades = outcomes |> Seq.length
        let winners = outcomes |> Seq.filter (fun o -> o.PercentGain > 0m)
        let losers = outcomes |> Seq.filter (fun o -> o.PercentGain < 0m)
        let numberOfWinners = winners |> Seq.length
        let numberOfLosers = losers |> Seq.length
        let win_pct = decimal numberOfWinners / decimal numberOfTrades
        let avg_win =
            match numberOfWinners with
            | 0 -> 0m
            | _ -> winners |> Seq.averageBy (_.PercentGain)
        let avg_loss =
            match numberOfLosers with
            | 0 -> 0m
            | _ -> losers |> Seq.averageBy (_.PercentGain)
        let avg_gain_loss =
            match avg_loss with
            | 0m -> 0m
            | _ -> avg_win / avg_loss |> Math.Abs
        let ev = win_pct * avg_win - (1m - win_pct) * (avg_loss |> Math.Abs)
        let totalGain = outcomes |> Seq.sumBy (_.PercentGain)

        let gains = outcomes |> Seq.map (_.PercentGain)
        let gainDistribution = DistributionStatistics.calculate gains

        // return trade summary
        {
            StrategyName = name
            NumberOfTrades = numberOfTrades
            Winners = numberOfWinners
            Losers = numberOfLosers
            TotalGain = totalGain
            WinPct = win_pct
            AvgWin = avg_win
            AvgLoss = avg_loss
            AvgGainLoss = avg_gain_loss
            EV = ev
            Gains = gains
            GainDistribution = gainDistribution
        }
let private getEarliestDateByTicker (records:Signal.Row seq) =
    
    records
        |> Seq.groupBy _.Ticker
        |> Seq.map (fun (ticker, records) ->
            let earliestDate = records |> Seq.minBy _.Date
            (ticker, earliestDate)
        )
    
let private fetchPriceFeeds (brokerage:IGetPriceHistory) studiesDirectory tickerDatePairs = async {
    
    let runFetch() =
        tickerDatePairs
        |> Seq.map (fun (ticker, earliestDate:Signal.Row) -> async {
            
            let earliestDateMinus365 = earliestDate.Date |> DateTimeOffset.Parse |> _.AddDays(-365) |> Some
            let today = DateTimeOffset.UtcNow |> Some
            
            // first try to get prices from local file
            let! prices = tryGetPricesFromCsv studiesDirectory ticker
            match prices with
            | Available _ -> return (ticker, prices)
            | NotAvailableForever -> return (ticker, prices)
            | _ ->
                // if not available, try pinging brokerage and record to csv 
                let! prices = ticker |> Ticker |> getPricesFromBrokerageAndRecordToCsv brokerage studiesDirectory earliestDateMinus365 today
                return (ticker, prices)
        })
        |> Async.Sequential
        
    // run the fetch at least 10 times, until there are non NotAvailable records left
    let rec runFetchUntilAllAvailable (count:int) = async {
        let! results = runFetch()
        let failed = results |> Seq.filter (fun (_, prices) -> match prices with | NotAvailable -> true | _ -> false) |> Seq.length
        if failed = 0 || count = 0 then
            return results
        else
            callLogFuncIfSetup _.LogCritical($"Failed to get {failed} prices, retrying...")
            return! runFetchUntilAllAvailable (count - 1)
    }   
            
    return! runFetchUntilAllAvailable 10
}
        
let transformSignals (brokerage:IGetPriceHistory) studiesDirectory signals = async {
        
    // generate a pair of ticker and the earliest data it is seen
    let tickerDatePairs = signals |> getEarliestDateByTicker
    
    // output how many records are left
    printfn $"Unique tickers: %d{tickerDatePairs |> Seq.length}"
    
    // when ready, for each ticker, get historical prices from price provider
    // starting with 365 days before the earliest date through today
    
    let! results = fetchPriceFeeds brokerage studiesDirectory tickerDatePairs
        
    let failed = results |> Array.filter (fun (_, prices) -> match prices with | Available _ -> false | _ -> true)
    let prices =
        results
        |> Array.choose (fun (ticker, prices) ->
            match prices with
            | Available prices ->
                Some (ticker, (prices, prices |> MovingAveragesContainer.Generate))
            | _ -> None
        )
        |> Map.ofArray
    
    printfn $"Failed: %d{failed.Length}"
    printfn $"Succeeded: %d{prices.Count}"
    
    let signalsWithPrices =
        signals
        |> Seq.filter (fun r -> prices.ContainsKey(r.Ticker))
        |> Seq.filter (fun r ->
            let ticker = r.Ticker
            let date = r.Date
            let prices, _ = prices[ticker]
            let signalBarWithIndex = prices.TryFindByDate date
            match signalBarWithIndex with
            | None ->
                // failwith $"Could not find signal bar for {ticker} on {date}"
                false
            | Some (index, _) -> 
                let nextDay = index + 1
                let nextDayBar = prices.Bars |> Array.tryItem nextDay
                match nextDayBar with
                | Some _ -> true
                | None -> false
        )
        
    printfn $"Records with prices: %d{signalsWithPrices |> Seq.length}"
    
    // now we are interested in gap ups
    let gapIndex =
        prices
        |> Map.keys
        |> Seq.collect (fun ticker ->
            let bars, _ = prices[ticker]
            let gaps = bars |> detectGaps bars.Length
            gaps
            |> Array.map (fun (g:Gap) ->
                let gapKey = (ticker, g.Bar.DateStr)
                (gapKey,g)
            )
        )
        |> Map.ofSeq
        
        
    printfn $"Gap up index: %d{gapIndex.Count}"
    
    // go through the signals and add gap information if found
    let transformed =
        signalsWithPrices
        |> Seq.map (fun r ->
            let key = (r.Ticker, r.Date)
            let gapSize =
                match gapIndex.TryGetValue(key) with
                | false, _ -> None
                | true, g -> Some g
            (r, gapSize)
        )
    
    printfn $"Updated records: %d{transformed |> Seq.length}"
    
    let findSmaValue index smaValues =
        match smaValues |> Array.tryItem index with
        | Some v ->
            match v with | Some v -> v | None -> 0m
        | None -> 0m
    
    let rows =
        transformed
        |> Seq.map (fun (r,g) ->
            let gapSize = match g with | None -> 0m | Some g -> g.GapSizePct
            let prices, container = prices[r.Ticker]
            let index,_ = prices.TryFindByDate r.Date |> Option.get
            let sma20 = container.sma20.Values |> findSmaValue index
            let sma50 = container.sma50.Values |> findSmaValue index
            let sma150 = container.sma150.Values |> findSmaValue index
            let sma200 = container.sma200.Values |> findSmaValue index
            
            let row = SignalWithPriceProperties.Row(
                ticker=r.Ticker,
                date=r.Date,
                screenerid=r.Screenerid,
                gap=gapSize,
                sma20=sma20,
                sma50=sma50,
                sma150=sma150,
                sma200=sma200)
            row
        )
    
    return new SignalWithPriceProperties(rows)
}

module Trading =

    let private validateStopLossPercent stopLossPercent =
        match stopLossPercent with
        | Some stopLossPercent when stopLossPercent > 1m -> failwith $"Stop loss percent {stopLossPercent} is greater than 1"
        | Some stopLossPercent when stopLossPercent < 0m -> failwith $"Stop loss percent {stopLossPercent} is less than 0"
        | _ -> ()
            
    let private findNextDayBarAndIndex (signal:SignalWithPriceProperties.Row) (prices:PriceBars) =
        match prices.TryFindByDate signal.Date with
        | None -> failwith $"Could not find the price bar for the {signal.Ticker} @ {signal.Date}, unexpected"
        | Some openBarWithIndex ->
            let index,_ = openBarWithIndex
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
        let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> snd |> _.Open
        
        let stopLossFunc (index, bar:PriceBar) =
            let stopPriceReached = bar.Close < stopPrice
            let closeDayReached = bar.Date >= closeBar.Date
            (index, bar, stopPriceReached, closeDayReached)
        
        strategyWithGenericStopLoss verbose name core.fs.Stocks.Long stopLossFunc (signal,prices)
        
    let strategyWithSignalCloseAsStop verbose (signal:SignalWithPriceProperties.Row,prices:PriceBars) =
        
        let name = "Buy and use signal close as stop"
            
        let closeBar = prices.Last        
        let stopPrice = prices.TryFindByDate signal.Date |> Option.get |> snd |> _.Close
        
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
                    Math.Max(bar.Close, stopLossReferencePrice.Value), bar.Close < stopLossReferencePrice.Value * (1m - stopLossPercent)
                | core.fs.Stocks.StockPositionType.Short ->
                    Math.Min(bar.Close, stopLossReferencePrice.Value), bar.Close > stopLossReferencePrice.Value * (1m + stopLossPercent)
                    
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
            |> Seq.sortBy (fun (r,_) -> r.Date)
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
                
                // track outcomes by ticker and open/close dates
                let map = Dictionary<string, List<TradeOutcomeOutput.Row>>()
                
                signalsWithPriceBars
                |> List.map strategy
                |> List.map (fun (outcome:TradeOutcomeOutput.Row) ->
                    match map.TryGetValue(outcome.Ticker) with
                    | false, _ ->
                        let list = List<TradeOutcomeOutput.Row>()
                        list.Add(outcome)
                        map.Add(outcome.Ticker, list)
                        Some outcome
                    | true, list ->
                        let fallsInsideExistingTrade = list |> Seq.exists (fun (x:TradeOutcomeOutput.Row) -> outcome.Opened >= x.Opened && outcome.Opened <= x.Closed)
                        match fallsInsideExistingTrade with
                        | true -> None
                        | false ->
                            list.Add(outcome)
                            Some outcome
                )
                |> List.choose id
            )
            |> List.concat
            
        return allOutcomes
    }

let run (context:EnvironmentContext) =
    
    let user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()

    let studiesDirectory = context.GetArgumentValue "-d"
        
    let actions = [
        if context.HasArgument "-i" then fun () -> async {
            let importUrl = context.GetArgumentValue "-i"
            let! response = Http.AsyncRequest(importUrl)
            let csv =
                match response.Body with
                | Text text -> text
                | _ -> failwith "Unexpected response from screener"
            let outputFilename = context.GetArgumentValue "-o"
            do! csv |> saveCsv outputFilename
        }
        
        if context.HasArgument "-pt" then fun () -> async {
            
            let priceProvider = context.PriceInfoProvider()    
            let pricesWrapper =
                {
                    new IGetPriceHistory with 
                        member this.GetPriceHistory start ``end`` ticker =
                            priceProvider.GetPriceHistory user.Value.State ticker PriceFrequency.Daily start ``end``
                }
            
            let! transformed =
                context.GetArgumentValue "-f"
                |> Signal.Load |> _.Rows
                |> transformSignals pricesWrapper studiesDirectory
                
            let outputFilename = context.GetArgumentValue "-o"
            do! transformed.SaveToString() |> appendCsv outputFilename
        }
    ]

    actions
        |> List.map (fun a -> a())
        |> Async.Sequential
        |> Async.RunSynchronously
        |> ignore
