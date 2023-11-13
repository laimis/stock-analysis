module studies.GapStudy

open System
open FSharp.Data

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
            let screenerid = Int32.Parse row["screenerid"]

            { date = date; ticker = ticker; screenerid = screenerid }
        )
        |> Seq.toArray

let verifyInput records =
    // make sure there is at least some records in here, ideally in thousands
    let numberOfRecords = records |> Array.length
    match numberOfRecords with
    | 0 -> failwith "no records"
    | x when x < 10000 -> failwith "not enough records"
    | _ -> ()
    
    // make sure no data is blank
    let dateFine =
        records |> Array.forall (fun r -> r.date.Year > 2020 && r.date.Year < 2024)
    
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
        
let run inputFilename =
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