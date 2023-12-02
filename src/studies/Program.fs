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
            let strategies = [
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 5) None
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 10) None
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 30) None
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 60) None
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false (Some 90) None
                TradingStrategies.buyAndHoldStrategyWithStopLossPercent false None None
            ]
    
            let! signalsWithPriceBars = Trading.prepareSignalsForTradeSimulations outputFilename getPricesFromCsv
            let! outcomes = Trading.runTrades signalsWithPriceBars strategies 
            outcomes |> Types.TradeOutcomeOutput.save tradeOutcomesFilename
        }
]

actions
    |> List.map (fun a -> a())
    |> Async.Sequential
    |> Async.RunSynchronously
    |> ignore