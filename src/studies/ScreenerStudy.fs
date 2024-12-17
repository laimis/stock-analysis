module studies.ScreenerStudy

open System
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
    
type SignalWrapper(row:Signal.Row) =
    interface ISignal with
        member this.Ticker = row.Ticker
        member this.Date = row.Date
        member this.Screenerid = row.Screenerid |> Some
        
type SignalWithPriceProperties =
    CsvProvider<
        Schema = "screenerid (int), date (string), ticker (string), open (decimal), close (decimal), high (decimal), low (decimal), gap (decimal), sma20 (decimal), sma50 (decimal), sma150 (decimal), sma200 (decimal)",
        HasHeaders=false
    >
    
type SignalWithPricePropertiesWrapper(row:SignalWithPriceProperties.Row) =
    interface ISignal with
        member this.Ticker = row.Ticker
        member this.Date = row.Date
        member this.Screenerid = row.Screenerid |> Some
        

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
            | Ok _ -> return (ticker, prices)
            | Error err ->
                match err with
                | NotAvailableForever _ ->
                    return (ticker, prices)
                | NotAvailable _ ->
                    // if not available but temporarily, try pinging brokerage and record to csv 
                    let! prices = ticker |> Ticker |> getPricesFromBrokerageAndRecordToCsv brokerage studiesDirectory earliestDateMinus365 today
                    return (ticker, prices)
        })
        |> Async.Sequential
        
    // run the fetch at least 10 times, until there are non NotAvailable records left
    let rec runFetchUntilAllAvailable (count:int) = async {
        let! results = runFetch()
        let failed = results |> Seq.filter (fun (_, prices) -> match prices with | Error _ -> true | _ -> false) |> Seq.length
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
    callLogFuncIfSetup _.LogInformation($"Unique tickers: %d{tickerDatePairs |> Seq.length}")
    
    // when ready, for each ticker, get historical prices from price provider
    // starting with 365 days before the earliest date through today
    
    let! results = fetchPriceFeeds brokerage studiesDirectory tickerDatePairs
        
    let failed = results |> Array.filter (fun (_, prices) -> match prices with | Error _ -> true | _ -> false)
    let prices =
        results
        |> Array.choose (fun (ticker, prices) ->
            match prices with
            | Ok prices ->
                Some (ticker, (prices, prices |> MovingAveragesContainer.Generate))
            | _ -> None
        )
        |> Map.ofArray
    
    callLogFuncIfSetup _.LogInformation($"Failed: %d{failed.Length}")
    callLogFuncIfSetup _.LogInformation($"Succeeded: %d{prices.Count}")
    
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
        
    callLogFuncIfSetup _.LogInformation($"Records with prices: %d{signalsWithPrices |> Seq.length}")
    
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
        
        
    callLogFuncIfSetup _.LogInformation($"Gap up index: %d{gapIndex.Count}")
    
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
    
    callLogFuncIfSetup _.LogInformation($"Updated records: %d{transformed |> Seq.length}")
    
    let getValueAtIndex index values =
        match values |> Array.tryItem index with
        | Some v ->
            match v with | Some v -> v | None -> 0m
        | None -> 0m
    
    let rows =
        transformed
        |> Seq.map (fun (r,g) ->
            let gapSize = match g with | None -> 0m | Some g -> g.GapSizePct
            let prices, container = prices[r.Ticker]
            let index,price = prices.TryFindByDate r.Date |> Option.get
            let ema20 = container.ema20.Values |> getValueAtIndex index
            let sma20 = container.sma20.Values |> getValueAtIndex index
            let sma50 = container.sma50.Values |> getValueAtIndex index
            let sma150 = container.sma150.Values |> getValueAtIndex index
            let sma200 = container.sma200.Values |> getValueAtIndex index
            
            let row = SignalWithPriceProperties.Row(
                ticker=r.Ticker,
                date=r.Date,
                screenerid=r.Screenerid,
                ``open``=price.Open,
                close=price.Close,
                high=price.High,
                low=price.Low,
                gap=gapSize,
                sma20=sma20,
                sma50=sma50,
                sma150=sma150,
                sma200=sma200)
            row
        )
    
    return new SignalWithPriceProperties(rows)
}

let private priceTransform (context:EnvironmentContext) userState = async {
    let studiesDirectory = context.GetArgumentValue "-d"
        
    let brokerage = context.Brokerage()    
    let pricesWrapper =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker =
                    brokerage.GetPriceHistory userState ticker PriceFrequency.Daily start ``end``
        }
    
    let! transformed =
        context.GetArgumentValue "-f"
        |> Signal.Load |> _.Rows
        |> transformSignals pricesWrapper studiesDirectory
        
    let outputFilename = context.GetArgumentValue "-o"
    do! transformed.SaveToString() |> appendCsv outputFilename
}

let private importData (context:EnvironmentContext) = async {
    let importUrl = context.GetArgumentValue "-i"
    let! response = Http.AsyncRequest(importUrl)
    let csv =
        match response.Body with
        | Text text -> text
        | _ -> failwith "Unexpected response from screener"
    let outputFilename = context.GetArgumentValue "-o"
    do! csv |> saveCsv outputFilename
}

let run (context:EnvironmentContext) =
    
    let user = "laimis@gmail.com" |> context.Storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
    match user with
    | None -> failwith "User not found"
    | Some _ -> ()

    let actions = [
        if context.HasArgument "-i" then fun () -> async {
            do! importData context
        }
        
        if context.HasArgument "-pt" then fun () -> async {
            do! priceTransform context user.Value.State
        }
    ]

    actions
        |> List.map (fun a -> a())
        |> Async.Sequential
        |> Async.RunSynchronously
        |> ignore
