module studiestests.TradingTests

open System
open Xunit
open FsUnit
open core.fs.Stocks
open studies
open studies.DataHelpers
open studies.ScreenerStudy
open testutils


let round (number:decimal) = Math.Round(number, 4)

let runTradesSetupWithSignals signals strategies = async {
    let getPricesFunc = DataHelpersTests.setupGetPricesWithNoBrokerageAccess
    
    let! transformed = transformSignals getPricesFunc DataHelpersTests.testDataPath signals
    
    let signals = transformed.Rows |> Seq.map SignalWithPricePropertiesWrapper |> Seq.cast<ISignal>
    
    let! outcomes = Trading.runTrades (DataHelpers.getPricesFromCsv TestDataGenerator.TestDataPath) signals strategies
    
    return outcomes
}

let runTradesSetupWithSpecificEntryDate date =
    [Signal.Row(date = date, ticker = "NET", screenerid = 1)] |> runTradesSetupWithSignals

let runTradesSetup = runTradesSetupWithSpecificEntryDate "2022-08-05"

[<Fact>]
let ``Buy & Hold with trailing stop``() = async {
    let strategy = Trading.strategyWithTrailingStop false StockPositionType.Long 0.05m
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-08-08"
    outcome.Closed |> should equal "2022-08-17"
    outcome.Ticker |> should equal "NET"
    outcome.OpenPrice |> should equal 74.425m
    outcome.ClosePrice |> should equal 74.51m
    outcome.PercentGain |> round |> should equal 0.0011m
    outcome.NumberOfDaysHeld |> should equal 9
    outcome.Strategy |> should equal "Buy and use trailing stop, SL of 0.05%"
}

[<Fact>]
let ``Buy & Hold with signal close as stop``() = async {
    let strategy = Trading.strategyWithSignalCloseAsStop false
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-08-08"
    outcome.Closed |> should equal "2022-08-08"
    outcome.Ticker |> should equal "NET"
    outcome.OpenPrice |> should equal 74.425m
    outcome.ClosePrice |> should equal 73.68m
    outcome.PercentGain |> round |> should equal -0.01m
    outcome.NumberOfDaysHeld |> should equal 0
    outcome.Strategy |> should equal "Buy and use signal close as stop"
}

[<Fact>]
let ``Buy & Hold with signal open as stop``() = async {
    let strategy = Trading.strategyWithSignalOpenAsStop false
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-08-08"
    outcome.Closed |> should equal "2022-08-19"
    outcome.Ticker |> should equal "NET"
    outcome.OpenPrice |> should equal 74.425m
    outcome.ClosePrice |> should equal 68.53m
    outcome.PercentGain |> round |> should equal -0.0792m
    outcome.NumberOfDaysHeld |> should equal 11
    outcome.Strategy |> should equal "Buy and use signal open as stop"
}

[<Fact>]
let ``Buy & Hold with stop loss percent``() = async {
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Long None (Some 0.1m)
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-08-08"
    outcome.Closed |> should equal "2022-08-22"
    outcome.Ticker |> should equal "NET"
    outcome.OpenPrice |> should equal 74.425m
    outcome.ClosePrice |> should equal 65.78m
    outcome.PercentGain |> round |> should equal -0.1162m
    outcome.NumberOfDaysHeld |> should equal 14
    outcome.Strategy |> should equal "Buy SL of 0.10%"
}

[<Fact>]
let ``Buy & Hold for a fixed number of bars``() = async {
    
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Long (Some 30) None
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-08-08"
    outcome.Closed |> should equal "2022-09-20"
    outcome.Ticker |> should equal "NET"
    outcome.OpenPrice |> should equal 74.425m
    outcome.ClosePrice |> should equal 61.13m
    outcome.PercentGain |> round |> should equal -0.1786m
    outcome.NumberOfDaysHeld |> should equal 43
    outcome.Strategy |> should equal "Buy hold for 30 bars"
}

[<Fact>]
let ``Buy and hold with stop loss and number of bars really big, does not fail``() = async {
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Long (Some 100000) None
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Closed |> should equal "2022-11-30"
}

[<Fact>]
let ``Buy NET on 2021-05-20 and use 5% stop loss vs 5% trailing stop loss``() = async {
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Long None (Some 0.05m)
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2021-05-20"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2021-05-21"
    outcome.Closed |> should equal "2022-05-06"
    outcome.PercentGain |> round |> should equal -0.1266m
    
    let strategy = Trading.strategyWithTrailingStop false StockPositionType.Long 0.05m
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2021-05-20"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2021-05-21"
    outcome.Closed |> should equal "2021-07-15"
    outcome.PercentGain |> round |> should equal 0.3667m
}

[<Fact>]
let ``Stop loss percent should be 1 or less``() = async {
    try
        do! [Trading.strategyWithStopLossPercent false StockPositionType.Long None (Some 1.1m)]
            |> runTradesSetup
            |> Async.Ignore
            
        failwith "Above should have failed"
    with
        | e -> e.Message |> should equal "Stop loss percent 1.1 is greater than 1"
}

[<Fact>]
let ``Stop loss percent should be 0 or greater``() = async {
    try
        do! [Trading.strategyWithStopLossPercent false StockPositionType.Long None (Some -0.1m)]
            |> runTradesSetup
            |> Async.Ignore
            
        failwith "Above should have failed"
    with
        | e -> e.Message |> should equal "Stop loss percent -0.1 is less than 0"
}

[<Fact>]
let ``Short positions with stop loss should work``() = async {
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Short None (Some 0.05m)
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2022-04-22"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-04-25"
    outcome.Closed |> should equal "2022-11-30"
    outcome.OpenPrice |> should equal 94.56m
    outcome.ClosePrice |> should equal 49.14m
    outcome.PercentGain |> round |> should equal 0.4803m
    outcome.NumberOfDaysHeld |> should equal 219
    outcome.Strategy |> should equal "Sell SL of 0.05%"
}

[<Fact>]
let ``Short positions with stop loss not set, should go to the end or to number of bars``() = async {
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Short None None
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2022-04-22"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-04-25"
    outcome.Closed |> should equal "2022-11-30"
    
    // now try with number of bars
    let strategy = Trading.strategyWithStopLossPercent false StockPositionType.Short (Some 20) None
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2022-04-22"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-04-25"
    outcome.Closed |> should equal "2022-05-23"
}

[<Fact>]
let ``Short position with trailing stop works``() = async {
    let strategy = Trading.strategyWithTrailingStop false StockPositionType.Short 0.1m
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2022-04-22"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2022-04-25"
    outcome.Closed |> should equal "2022-05-13"
    outcome.OpenPrice |> should equal 94.56m
    outcome.ClosePrice |> should equal 66.38m
    outcome.PercentGain |> round |> should equal 0.298m
    outcome.NumberOfDaysHeld |> should equal 18
    outcome.Strategy |> should equal "Sell and use trailing stop, SL of 0.1%"
}

[<Fact(Skip = "Run this to troubleshoot specific studies not working")>]
let ``Specific test``() = async {
    let signals = [Signal.Row(date = "2023-08-16", ticker = "PSFE", screenerid = 1)]
    
    let mock =
        {
            new IGetPriceHistory with 
                member this.GetPriceHistory start ``end`` ticker = task {
                    return [||] |> core.fs.Adapters.Stocks.PriceBars |> Ok
                    }
        }
        
    let! transformed = transformSignals mock "d:\\studies\\" signals
    
    let signals = transformed.Rows |> Seq.map SignalWithPricePropertiesWrapper |> Seq.cast<ISignal>
    
    let strategies = [Trading.strategyWithStopLossPercent false StockPositionType.Short None None]
    
    let! outcomes = Trading.runTrades (DataHelpers.getPricesFromCsv "d:\\studies\\") signals strategies
    
    let outcome = outcomes |> List.head
    
    // PSFE Sell: 2023-08-17 -> 2023-08-17, 13.49 -> 12.29: profit of 8.9%, signal: Top Losers
    
    outcome.Opened |> should equal "2023-08-17"
    outcome.Closed |> should equal "2023-08-17"
}

[<Fact>]
let ``Using signal that doesn't have a price bar, ignores it`` () = async {
    let signals = [
        Signal.Row(date = "2022-08-06", ticker = "NET", screenerid = 1)
    ]
    
    let strategies = [
        Trading.strategyWithStopLossPercent false StockPositionType.Long None None
    ]
    
    let! outcomes = strategies |> runTradesSetupWithSignals signals
    
    outcomes |> List.length |> should equal 0
}

[<Fact>]
let ``Trading multiple signals that overlap in dates, should return only non overlapping trades`` () = async {
    let signals = [
        Signal.Row(date = "2022-08-05", ticker = TestDataGenerator.NET.Value, screenerid = 1)
        Signal.Row(date = "2022-08-08", ticker = TestDataGenerator.NET.Value, screenerid = 2)
        Signal.Row(date = "2022-08-09", ticker = TestDataGenerator.NET.Value, screenerid = 2)
        Signal.Row(date = "2022-08-09", ticker = TestDataGenerator.AMD.Value, screenerid = 2)
        Signal.Row(date = "2022-08-10", ticker = TestDataGenerator.NET.Value, screenerid = 2)
    ]
    
    let strategies = [
        Trading.strategyWithStopLossPercent false StockPositionType.Long (Some 2) None
    ]
    
    let! outcomes = strategies |> runTradesSetupWithSignals signals
    
    outcomes |> List.length |> should equal 2
}
