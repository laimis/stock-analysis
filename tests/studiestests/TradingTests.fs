module studiestests.TradingTests

open System
open Xunit
open FsUnit
open studies
open studies.Types
open testutils

let round (number:decimal) = Math.Round(number, 4)

let runTradesSetupWithSpecificEntryDate date strategies = async {
    let signals = [Signal.Row(date = date, ticker = "NET", screenerid = 1)]
    
    let getPricesFunc = DataHelpersTests.setupGetPricesWithNoBrokerageAccess
    
    let! transformed = PriceTransformation.transform getPricesFunc signals
    
    let! outcomes = Trading.runTrades (DataHelpers.getPricesFromCsv TestDataGenerator.TestDataPath) transformed.Rows strategies
    
    return outcomes
}

let runTradesSetup = runTradesSetupWithSpecificEntryDate "2022-08-05"

[<Fact>]
let ``Buy & Hold with trailing stop``() = async {
    let strategy = Trading.buyAndHoldWithTrailingStop false
    
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
    outcome.Strategy |> should equal "B&H with trailing stop"
}

[<Fact>]
let ``Buy & Hold with signal close as stop``() = async {
    let strategy = Trading.buyAndHoldWithSignalCloseAsStop false
    
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
    outcome.Strategy |> should equal "B&H with signal close as stop"
}

[<Fact>]
let ``Buy & Hold with signal open as stop``() = async {
    let strategy = Trading.buyAndHoldWithSignalOpenAsStop false
    
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
    outcome.Strategy |> should equal "B&H with signal open as stop"
}

[<Fact>]
let ``Buy & Hold with stop loss percent``() = async {
    let strategy = Trading.buyAndHoldStrategyWithStopLossPercent false None (Some 0.1m)
    
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
    outcome.Strategy |> should equal "B&H SL of 0.10%"
}

[<Fact>]
let ``Buy & Hold for a fixed number of bars``() = async {
    
    let strategy = Trading.buyAndHoldStrategyWithStopLossPercent false (Some 30) None
    
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
    outcome.Strategy |> should equal "B&H 30 bars"
}

[<Fact>]
let ``Trade output summary works`` () = async {
    let strategies = [
        Trading.buyAndHoldWithTrailingStop true
        Trading.buyAndHoldStrategyWithStopLossPercent true (Some 30) None
    ]
    
    let! outcomes = strategies |> runTradesSetup
    
    let outcome = outcomes[0]
    
    let summary = TradeSummary.create outcome.Strategy outcomes
    
    summary.Gains |> Seq.map (fun g -> Math.Round(g, 4)) |> should equal [0.0011m; -0.1786m]
    summary.Losers |> should equal 1
    summary.Total |> should equal 2
    summary.Winners |> should equal 1
    summary.AvgLoss |> round |> should equal -0.1786m
    summary.AvgWin |> round |> should equal 0.0011m
    summary.EV  |> round |> should equal -0.0887m 
    summary.StrategyName |> should equal outcome.Strategy
    summary.WinPct |> round |> should equal 0.5m
    summary.AvgGainLoss |> round |> should equal 0.0064m
}

[<Fact>]
let ``Buy and hold with stop loss and number of bars really big, does not fail``() = async {
    let strategy = Trading.buyAndHoldStrategyWithStopLossPercent false (Some 100000) None
    
    let! outcomes = [strategy] |> runTradesSetup
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Closed |> should equal "2022-11-30"
}

[<Fact>]
let ``Buy NET on 2021-05-20 and use 5% stop loss vs 5% trailing stop loss``() = async {
    let strategy = Trading.buyAndHoldStrategyWithStopLossPercent false None (Some 0.05m)
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2021-05-20"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2021-05-21"
    outcome.Closed |> should equal "2022-05-06"
    outcome.PercentGain |> round |> should equal -0.1266m
    
    let strategy = Trading.buyAndHoldWithTrailingStop false
    
    let! outcomes = [strategy] |> runTradesSetupWithSpecificEntryDate "2021-05-20"
    
    outcomes |> List.length |> should equal 1
    
    let outcome = outcomes[0]
    
    outcome.Opened |> should equal "2021-05-21"
    outcome.Closed |> should equal "2021-07-15"
    outcome.PercentGain |> round |> should equal 0.3667m
}