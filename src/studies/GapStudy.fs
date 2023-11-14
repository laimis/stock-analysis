module studies.GapStudy

open System
open FSharp.Data
open core.Shared
open core.fs.Shared
open core.fs.Shared.Adapters.Stocks

[<Literal>]
let private minimumRecords = 10_000
[<Literal>]
let private earliestYear = 2020
[<Literal>]
let private latestYear = 2024

let private midnight = TimeOnly.Parse "00:00:00"

// "date","ticker","screenerid"
type Input = {
    date: DateOnly
    ticker: string
    screenerid: int
}

let parseInput (inputFilename:string) =
    let csvFile = CsvFile.Load(inputFilename)

    csvFile.Rows
        |> Seq.map (fun row ->
            let ticker = row["ticker"]
            let date = DateOnly.Parse row["date"]
            let screenerid = row["screenerid"].AsInteger()

            { date = date; ticker = ticker; screenerid = screenerid }
        )
        |> Seq.toArray

let verifyInput records =
    // make sure there is at least some records in here, ideally in thousands
    let numberOfRecords = records |> Array.length
    match numberOfRecords with
    | 0 -> failwith "no records"
    | x when x < minimumRecords -> failwith "not enough records"
    | _ -> ()
    
    // make sure no data is blank
    let dateFine =
        records |> Array.forall (fun r -> r.date.Year > earliestYear && r.date.Year < latestYear)
    
    match dateFine with
    | false -> failwith "date is not in range"
    | true -> ()
    
    // make sure all tickers are set
    let tickersFine =
        records |> Array.forall (fun r -> String.IsNullOrWhiteSpace(r.ticker) = false)
        
    match tickersFine with
    | false -> failwith "ticker is blank"
    | true -> ()
    
    // make sure all screenerIds are set
    let screenerIdsFine =
        records |> Array.forall (fun r -> r.screenerid <> 0)
        
    match screenerIdsFine with
    | false -> failwith "screenerid is blank"
    | true -> ()
    
    records
    
let describeInput records =
    let numberOfRecords = records |> Array.length
    let dates = records |> Array.map (fun r -> r.date) |> Array.distinct |> Array.length
    let tickers = records |> Array.map (fun r -> r.ticker) |> Array.distinct |> Array.length
    let screenerIds = records |> Array.map (fun r -> r.screenerid) |> Array.distinct |> Array.length
    
    let minimumDate = records |> Array.minBy (fun r -> r.date)
    let maximumDate = records |> Array.maxBy (fun r -> r.date)
    
    printfn $"Records: %d{numberOfRecords}, dates: %d{dates}, tickers: %d{tickers}, screenerIds: %d{screenerIds}"
    printfn $"Minimum date: %A{minimumDate.date}"
    printfn $"Maximum date: %A{maximumDate.date}"
    printfn ""
    
    records
    
let getEarliestDateByTicker records =
    
    records
        |> Array.groupBy (fun r -> r.ticker)
        |> Array.map (fun (ticker, records) ->
            let earliestDate = records |> Array.minBy (fun r -> r.date)
            (ticker, earliestDate)
        )
        
let run inputFilename (priceFunc:DateTimeOffset -> DateTimeOffset -> Ticker -> Async<PriceBars option>) = async {
    // parse and verify
    let records =
        inputFilename
        |> parseInput
        |> verifyInput
        |> describeInput

    // generate a pair of ticker and the earliest data it is seen
    let tickerDatePairs = records |> getEarliestDateByTicker
    
    // output how many records are left
    printfn $"Unique tickers: %d{tickerDatePairs.Length}"
    
    // when ready, for each ticker, get historical prices from price provider
    // starting with 365 days before the earliest date through today
    
    let! results =
        tickerDatePairs
        |> Array.map (fun (ticker, earliestDate) -> async {
            
            let earliestDateMinus365 = DateTimeOffset(earliestDate.date.AddDays(-365).ToDateTime(midnight))
            let today = DateTimeOffset.UtcNow
            
            let! prices = priceFunc earliestDateMinus365 today (ticker |> Ticker)
            return (ticker, prices)
        })
        |> Async.Sequential
        
    let failed = results |> Array.filter (fun (_, prices) -> prices.IsNone)
    let succeeded = results |> Array.filter (fun (_, prices) -> prices.IsSome)
    
    printfn $"Failed: %d{failed.Length}"
    printfn $"Succeeded: %d{succeeded.Length}"
}