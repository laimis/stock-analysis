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

let getPricesWithBrokerage = DataHelpers.getPricesWithBrokerage user.Value (ServiceHelper.brokerage()) studiesDirectory
let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory

match ServiceHelper.hasArgument "-s" with
| true ->
    printfn "Running study"
    GapStudy.study inputFilename outputFilename getPricesWithBrokerage |> Async.RunSynchronously
| false ->
    ()

match ServiceHelper.hasArgument "-t" with
| true -> 
    GapStudy.runTrades outputFilename getPricesFromCsv
| false ->
    ()