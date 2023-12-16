module studies.GapStudy

open System
open core.Shared
open core.fs.Adapters.Stocks
open core.fs.Services.GapAnalysis
open studies.Types



let parseTradeOutcomes (filepath:string) = TradeOutcomeOutput.Load(filepath).Rows

let verifyRecords (input:StudyInput) =
    let records = input.Rows
    // make sure there is at least some records in here, ideally in thousands
    let numberOfRecords = records |> Seq.length
    match numberOfRecords with
    | 0 -> failwith "no records"
    | x when x < Constants.MinimumRecords -> failwith $"not enough records: {x}"
    | _ -> ()
    
    // make sure all dates are as expected
    let datesFine = records |> Seq.forall (fun r -> r.Date.Year >= Constants.EarliestYear && r.Date.Year <= Constants.LatestYear)
    match datesFine with
    | false -> failwith "date is out of range"
    | true -> ()
    
    // make sure all tickers are set
    let tickersFine = records |> Seq.forall (fun r -> String.IsNullOrWhiteSpace(r.Ticker) = false)
    match tickersFine with
    | false -> failwith "ticker is blank"
    | true -> ()
    
    // make sure all screenerIds are set
    let screenerIdsFine = records |> Seq.forall (fun r -> r.Screenerid <> 0)
    match screenerIdsFine with
    | false -> failwith "screenerid is blank"
    | true -> ()
    
    records
    
let getEarliestDateByTicker (records:StudyInput.Row seq) =
    
    records
        |> Seq.groupBy (fun r -> r.Ticker)
        |> Seq.map (fun (ticker, records) ->
            let earliestDate = records |> Seq.minBy (fun r -> r.Date)
            (ticker, earliestDate)
        )
        
let study (inputFilename:string) (outputFilename:string) (priceFunc:DateTimeOffset -> DateTimeOffset -> Ticker -> Async<PriceBars option>) = async {
    // parse and verify
    let records =
        inputFilename
        |> StudyInput.Load
        |> verifyRecords
        
    // describe records
    records |> Seq.map (fun r -> Input r) |> Unified.describeRecords
        
    // generate a pair of ticker and the earliest data it is seen
    let tickerDatePairs = records |> getEarliestDateByTicker
    
    // output how many records are left
    printfn $"Unique tickers: %d{tickerDatePairs |> Seq.length}"
    
    // when ready, for each ticker, get historical prices from price provider
    // starting with 365 days before the earliest date through today
    
    let! results =
        tickerDatePairs
        |> Seq.map (fun (ticker, earliestDate) -> async {
            
            let earliestDateMinus365 = earliestDate.Date.AddDays(-365)
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
    
    let recordsWithPrices =
        records
        |> Seq.filter (fun r -> prices.ContainsKey(r.Ticker))
        |> Seq.filter (fun r ->
            let ticker = r.Ticker
            let date = r.Date
            let prices = prices[ticker]
            let signalBarWithIndex = prices.TryFindByDate date
            match signalBarWithIndex with
            | None ->
                // failwith $"Could not find signal bar for {ticker} on {date}"
                false
            | Some signalBarWithIndex -> 
                let nextDay = signalBarWithIndex |> snd |> fun x -> x + 1
                let nextDayBar = prices.Bars |> Array.tryItem nextDay
                match nextDayBar with
                | Some _ -> true
                | None -> false
        )
        
    printfn $"Records with prices: %d{recordsWithPrices |> Seq.length}"
    
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
        |> Seq.map (fun r ->
            let hasGapUp = gapUpIndex.ContainsKey(r.Ticker, r.Date.ToString("yyyy-MM-dd"))
            (r, hasGapUp)
        )
    
    printfn $"Updated records: %d{updatedRecords |> Seq.length}"
    
    // output records with gap ups into CSV
    let rows =
        updatedRecords
        |> Seq.map (fun (r,hasGapUp) ->
            let row = GapStudyOutput.Row(ticker=r.Ticker, date=r.Date, screenerid=r.Screenerid, hasGapUp=hasGapUp)
            row
        )
       
    let csvOutput = new GapStudyOutput(rows)
    csvOutput.Save outputFilename
}