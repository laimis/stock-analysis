// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open studies

Environment.GetCommandLineArgs() |> ServiceHelper.init

let user = "laimis@gmail.com" |> DataHelpers.getUser (ServiceHelper.storage()) |> Async.RunSynchronously
match user with
| None -> failwith "User not found"
| Some _ -> ()

let studiesDirectory = ServiceHelper.studiesDirectory
let inputFilename = $"{studiesDirectory}\\01_export_date_ticker_screenerid.csv"
let outputFilename = $"{studiesDirectory}\\02_export_date_ticker_screenerid_gap.csv"

let getPricesWithBrokerage = DataHelpers.getPricesWithBrokerage user.Value (ServiceHelper.brokerage()) studiesDirectory
let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory

GapStudy.study inputFilename outputFilename getPricesWithBrokerage |> Async.RunSynchronously

GapStudy.runTrades outputFilename getPricesFromCsv