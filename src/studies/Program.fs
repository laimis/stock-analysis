// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open FSharp.Data
open studies
open Microsoft.Extensions.Logging
open studies.Types

Environment.GetCommandLineArgs() |> ServiceHelper.init None

let user = "laimis@gmail.com" |> ServiceHelper.storage().GetUserByEmail |> Async.AwaitTask |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let studiesDirectory =
    match ServiceHelper.studiesDirectory() with
    | None ->
        ServiceHelper.logger.LogError("Studies directory not found")
        exit -1
    | Some d -> d
    
let generateFilePathInStudiesDirectory filename = $"{studiesDirectory}\\{filename}"

// -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
// -pt -f signals_topgainers.csv

let actions = [
    if ServiceHelper.importUrl().IsSome then fun () -> async {
        let importUrl = ServiceHelper.importUrl() |> Option.get
        let! response = Http.AsyncRequest(importUrl)
        let csv =
            match response.Body with
            | Text text -> text
            | _ -> failwith "Unexpected response from screener"
        let outputFilename = ServiceHelper.outputFilename() |> generateFilePathInStudiesDirectory
        do! csv |> DataHelpers.saveCsv outputFilename
    }
    if ServiceHelper.hasArgument "-pt" then fun () -> async {
        let getPricesWithBrokerage = DataHelpers.getPricesWithBrokerage user.Value (ServiceHelper.brokerage()) studiesDirectory
        
        let! transformed =
            ServiceHelper.inputFilename()
            |> generateFilePathInStudiesDirectory
            |> Signal.Load |> _.Rows
            |> PriceTransformation.transform getPricesWithBrokerage
            
        let outputFilename = ServiceHelper.outputFilename() |> generateFilePathInStudiesDirectory
        do! transformed.SaveToString() |> DataHelpers.appendCsv outputFilename
    }
    if ServiceHelper.hasArgument "-trade" then fun () -> async {
        let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory
        
        let strategies = [
            Trading.strategyWithStopLossPercent false (Some 5) None
            Trading.strategyWithStopLossPercent false (Some 10) None
            Trading.strategyWithStopLossPercent false (Some 30) None
            Trading.strategyWithStopLossPercent false (Some 60) None
            Trading.strategyWithStopLossPercent false (Some 90) None
            Trading.strategyWithStopLossPercent false None None
            Trading.strategyWithTrailingStop false
        ]
        
        let signals =
            ServiceHelper.inputFilename()
            |> generateFilePathInStudiesDirectory
            |> SignalWithPriceProperties.Load
            |> _.Rows
        
        let! outcomes = Trading.runTrades getPricesFromCsv signals strategies
        let csv = new TradeOutcomeOutput(outcomes)
        csv.Save($"{studiesDirectory}\\03_export_date_ticker_screenerid_gap_outcomes.csv")
    }
]

actions
    |> List.map (fun a -> a())
    |> Async.Sequential
    |> Async.RunSynchronously
    |> ignore