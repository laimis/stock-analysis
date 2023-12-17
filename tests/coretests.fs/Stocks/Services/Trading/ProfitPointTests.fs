module coretests.fs.Stocks.Services.Trading.ProfitPointTests

open System
open FsUnit
open Xunit
open core.fs.Services.Trading
open core.fs.Stocks
open coretests.testdata

let position =
    StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30.0m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35.0m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.setStop (Some 27.5m) (DateTimeOffset.UtcNow)
    |> StockPositionWithCalculations
    
[<Fact>]
let ``Profit levels work`` () =
    ProfitPoints.getProfitPointWithStopPrice 1 position |> should equal 37.5m
    ProfitPoints.getProfitPointWithStopPrice 2 position |> should equal 42.5m
    ProfitPoints.getProfitPointWithStopPrice 3 position |> should equal 47.5m
    ProfitPoints.getProfitPointWithStopPrice 4 position |> should equal 52.5m
    ProfitPoints.getProfitPointWithStopPrice 5 position |> should equal 57.5m

[<Fact>]
let ``Percent levels work`` () =
    ProfitPoints.getProfitPointWithPercentGain 1 0.05m position |> should equal 34.125m
    ProfitPoints.getProfitPointWithPercentGain 2 0.05m position |> should equal 35.75m
    ProfitPoints.getProfitPointWithPercentGain 3 0.05m position |> should equal 37.375m
    ProfitPoints.getProfitPointWithPercentGain 4 0.05m position |> should equal 39.0m
    ProfitPoints.getProfitPointWithPercentGain 5 0.05m position |> should equal 40.625m
