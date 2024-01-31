// Gap study
// read input from file 01_export_date_ticker_screenerid.csv
open System
open FSharp.Data
open studies
open Microsoft.Extensions.Logging
open studies.DataHelpers
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

let actions = [
    if ServiceHelper.importUrl().IsSome then fun () -> async {
        let importUrl = ServiceHelper.importUrl() |> Option.get
        let! response = Http.AsyncRequest(importUrl)
        let csv =
            match response.Body with
            | Text text -> text
            | _ -> failwith "Unexpected response from screener"
        let outputFilename = ServiceHelper.outputFilename()
        do! csv |> saveCsv outputFilename
    }
    
    if ServiceHelper.hasArgument "-pt" then fun () -> async {
        
        let brokerage = ServiceHelper.brokerage()    
        let pricesWrapper =
            {
                new IGetPriceHistory with 
                    member this.GetPriceHistory start ``end`` ticker =
                        brokerage.GetPriceHistory user.Value.State ticker core.fs.Adapters.Stocks.PriceFrequency.Daily start ``end``
            }
        
        let! transformed =
            ServiceHelper.inputFilename()
            |> Signal.Load |> _.Rows
            |> PriceTransformation.transform pricesWrapper studiesDirectory
            
        let outputFilename = ServiceHelper.outputFilename()
        do! transformed.SaveToString() |> appendCsv outputFilename
    }
    
    if ServiceHelper.hasArgument "-trade" then fun () -> async {
        let getPricesFromCsv = DataHelpers.getPricesFromCsv studiesDirectory    
        let strategies = [
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long (Some 5) None
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long (Some 10) None
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long (Some 30) None
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long (Some 60) None
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long (Some 90) None
            Trading.strategyWithStopLossPercent false core.fs.Stocks.Long None None
            Trading.strategyWithTrailingStop false core.fs.Stocks.Long 0.05m
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
