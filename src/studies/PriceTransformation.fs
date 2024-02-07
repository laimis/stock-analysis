module studies.PriceTransformation

open System
open core.Shared
open core.fs.Services.Analysis
open core.fs.Services.GapAnalysis
open studies.DataHelpers
open studies.Types
open Microsoft.Extensions.Logging
    
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
            
            let earliestDateMinus365 = earliestDate.Date |> DateTimeOffset.Parse |> _.AddDays(-365)
            let today = DateTimeOffset.UtcNow
            
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
        
let transform (brokerage:IGetPriceHistory) studiesDirectory signals = async {
        
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
                Some (ticker, (prices, prices |> SMAContainer.Generate))
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
            | Some (_, index) -> 
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
            let _,index = prices.TryFindByDate r.Date |> Option.get
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
