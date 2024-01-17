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
    
let generateFilePathInStudiesDirectory filename = $"{studiesDirectory}\\{filename}"
let tradeOutcomesFilename = $"{studiesDirectory}\\03_export_date_ticker_screenerid_gap_outcomes.csv"

// -i "https://localhost:5001/screeners/29/results/export" -o signals_topgainers.csv
// -pt -f signals_topgainers.csv

let actions = [
    if ServiceHelper.hasImportUrl() then fun () -> async {
        let! csv = ServiceHelper.importUrl() |> DataHelpers.getScreenerResults
        let outputFilename = ServiceHelper.outputFilename() |> generateFilePathInStudiesDirectory
        do! csv |> DataHelpers.saveCsv outputFilename
    }
    if ServiceHelper.hasArgument "-pt" then fun () -> async {
        let getPricesWithBrokerage = DataHelpers.getPricesWithBrokerage user.Value (ServiceHelper.brokerage()) studiesDirectory
        let inputFilename = ServiceHelper.inputFilename() |> generateFilePathInStudiesDirectory
        let outputFilename = ServiceHelper.outputFilename() |> generateFilePathInStudiesDirectory
        let! transformed = PriceTransformation.transform inputFilename getPricesWithBrokerage
        do! transformed.SaveToString() |> DataHelpers.appendCsv outputFilename
    }
    if ServiceHelper.hasArgument "-trade" then fun () -> async {
        let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory
        
        let strategies = [
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 5) None
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 10) None
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 30) None
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 60) None
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 90) None
            TradingStrategies.buyAndHoldStrategyWithStopLossPercent false None None
        ]
        
        let inputFilename = ServiceHelper.inputFilename() |> generateFilePathInStudiesDirectory

        let! signalsWithPriceBars = Trading.prepareSignalsForTradeSimulations inputFilename getPricesFromCsv
        let! outcomes = Trading.runTrades signalsWithPriceBars strategies 
        outcomes |> Types.TradeOutcomeOutput.save tradeOutcomesFilename
    }
]

actions
    |> List.map (fun a -> a())
    |> Async.Sequential
    |> Async.RunSynchronously
    |> ignore