module studies.GapStudy

open System
open FSharp.Data
open core.Shared
open core.fs.Services.GapAnalysis
open core.fs.Shared.Adapters.Stocks

[<Literal>]
let private minimumRecords = 1_000
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
    hasGapUp: bool option
}

type GapStudyOutput = CsvProvider<Schema = "ticker (string), date (string), screenerid (int), hasGapUp (bool option)", HasHeaders=false>

let parseInput (inputFilename:string) =
    let csvFile = CsvFile.Load(inputFilename)
    
    csvFile.Rows
        |> Seq.map (fun row ->
            let ticker = row["ticker"]
            let date = DateOnly.Parse row["date"]
            let screenerid = row["screenerid"].AsInteger()

            { date = date; ticker = ticker; screenerid = screenerid; hasGapUp = None }
        )
        |> Seq.toArray
        
let parseOutput (filepath:string) =
    
    let csvFile = GapStudyOutput.Load(filepath)
    
    csvFile.Rows
        |> Seq.map (fun row ->
            let ticker = row.Ticker
            let date = DateOnly.Parse row.Date
            let screenerid = row.Screenerid
            let hasGapUp = row.HasGapUp

            { date = date; ticker = ticker; screenerid = screenerid; hasGapUp = hasGapUp }
        )
        |> Seq.toArray

let verifyRecords records =
    // make sure there is at least some records in here, ideally in thousands
    let numberOfRecords = records |> Array.length
    match numberOfRecords with
    | 0 -> failwith "no records"
    | x when x < minimumRecords -> failwith $"not enough records: {x}"
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
    
let describeRecords records =
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
        
let study inputFilename (outputFilename:string) (priceFunc:DateTimeOffset -> DateTimeOffset -> Ticker -> Async<PriceBars option>) = async {
    // parse and verify
    let records =
        inputFilename
        |> parseInput
        |> verifyRecords
        |> describeRecords

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
            
            let! prices = ticker |> Ticker |> priceFunc earliestDateMinus365 today
            return (ticker, prices)
        })
        |> Async.Sequential
        
    let failed = results |> Array.filter (fun (_, prices) -> prices.IsNone)
    let prices =
        results
        |> Array.choose (fun (ticker, prices) ->
            match prices with
            | Some prices -> Some (ticker, prices)
            | None -> None
        )
        |> Map.ofArray
    
    printfn $"Failed: %d{failed.Length}"
    printfn $"Succeeded: %d{prices.Count}"
    
    let recordsWithPrices = records |> Array.filter (fun r -> prices.ContainsKey(r.ticker))
    printfn $"Records with prices: %d{recordsWithPrices.Length}"
    
    // now we are interested in gap ups
    let gapUpIndex =
        prices |> Map.keys |> Seq.collect (fun key ->
            let bars = prices[key]
            let gaps = detectGaps bars bars.Length
            let gapUps =
                gaps
                |> Array.filter (fun (g:Gap) -> g.Type = GapType.Up)
                |> Array.map (fun (g:Gap) ->
                    let gapKey = (key, g.Bar.DateStr)
                    (gapKey,g)
                )
            gapUps
        )
        |> Map.ofSeq
        
        
    printfn $"Gap up index: %d{gapUpIndex.Count}"
    
    // go through the records and only keep the ones that have a gap up
    let updatedRecords =
        recordsWithPrices
        |> Array.map (fun r ->
            let hasGapUp = gapUpIndex.ContainsKey(r.ticker, r.date.ToString("yyyy-MM-dd"))
            (r, hasGapUp)
        )
    
    printfn $"Updated records: %d{updatedRecords.Length}"
    
    // output records with gap ups into CSV
    let rows =
        updatedRecords
        |> Array.map (fun (r,hasGapUp) ->
            GapStudyOutput.Row(ticker=r.ticker, date=r.date.ToString("yyyy-MM-dd"), screenerid=r.screenerid, hasGapUp=Some hasGapUp)
        )
        
    let csvOutput = new GapStudyOutput(rows)
    csvOutput.Save outputFilename
}

let runTrades matchedInputFilename (priceFunc:string -> PriceBars) =
    
    let data =
         matchedInputFilename
         |> parseOutput
         |> verifyRecords
         |> describeRecords
    
    // ridiculous, sometimes input data does not have prices for the date
    // so we filter those records out
    let dataWithPriceBars =
        data
        |> Array.map (fun r ->
            let prices = r.ticker |> priceFunc
            let startBar = r.date |> prices.TryFindByDate
            (r, prices, startBar)
        )
        |> Array.choose (fun (r,prices,startBar) ->
            match startBar with
            | None -> None
            | Some startBar -> Some (r, prices, startBar)
        )
    
    printfn "Ensured that data has prices"
    
    dataWithPriceBars |> Array.map (fun (r, _, _) -> r) |> describeRecords |> ignore
       
    printfn "Executing trades... not implemented"
    
    ()
    