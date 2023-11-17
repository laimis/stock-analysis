// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open studies
open Microsoft.Extensions.Logging

Environment.GetCommandLineArgs() |> ServiceHelper.init

let user = "laimis@gmail.com" |> DataHelpers.getUser (ServiceHelper.storage()) |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let studiesDirectory =
    match ServiceHelper.studiesDirectory() with
    | None ->
        ServiceHelper.logger.LogError("Studies directory not found")
        exit -1
    | Some d -> d
    
let inputFilename = $"{studiesDirectory}\\01_export_date_ticker_screenerid.csv"
let outputFilename = $"{studiesDirectory}\\02_export_date_ticker_screenerid_gap.csv"
let tradeOutcomesFilename = $"{studiesDirectory}\\03_export_date_ticker_screenerid_gap_outcomes.csv"

let getPricesWithBrokerage = DataHelpers.getPricesWithBrokerage user.Value (ServiceHelper.brokerage()) studiesDirectory
let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory

let actions = [
    if ServiceHelper.hasArgument "-s" then fun () -> async {
            do! GapStudy.study inputFilename outputFilename getPricesWithBrokerage
        }
    if ServiceHelper.hasArgument "-t" then fun () -> async {
            let outcomes = GapStudy.runTrades outputFilename getPricesFromCsv
            outcomes |> GapStudy.saveOutcomes tradeOutcomesFilename
        }
]

actions
    |> List.map (fun a -> a())
    |> Async.Sequential
    |> Async.RunSynchronously
    |> ignore