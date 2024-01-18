module coretests.fs.Stocks.Services.Trading.ProfitPointTests

open System
open FsUnit
open Xunit
open core.fs.Services.Trading
open core.fs.Stocks
open testutils

let longPosition =
    StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30.0m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35.0m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.setStop (Some 27.5m) (DateTimeOffset.UtcNow)
    |> StockPositionWithCalculations
    
let shortPosition =
    StockPosition.openShort TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.sell 10m 30.0m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.sell 10m 35.0m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.setStop (Some 37.5m) (DateTimeOffset.UtcNow)
    |> StockPositionWithCalculations
    
[<Fact>]
let ``Profit levels work`` () =
    ProfitPoints.getProfitPointWithStopPrice 1 longPosition |> should equal 37.5m
    ProfitPoints.getProfitPointWithStopPrice 2 longPosition |> should equal 42.5m
    ProfitPoints.getProfitPointWithStopPrice 3 longPosition |> should equal 47.5m
    ProfitPoints.getProfitPointWithStopPrice 4 longPosition |> should equal 52.5m
    ProfitPoints.getProfitPointWithStopPrice 5 longPosition |> should equal 57.5m

[<Fact>]
let ``Percent levels work`` () =
    ProfitPoints.getProfitPointWithPercentGain 1 0.05m longPosition |> should equal 34.125m
    ProfitPoints.getProfitPointWithPercentGain 2 0.05m longPosition |> should equal 35.75m
    ProfitPoints.getProfitPointWithPercentGain 3 0.05m longPosition |> should equal 37.375m
    ProfitPoints.getProfitPointWithPercentGain 4 0.05m longPosition |> should equal 39.0m
    ProfitPoints.getProfitPointWithPercentGain 5 0.05m longPosition |> should equal 40.625m
    
[<Fact>]
let ``Profit levels for short position works`` () =
    ProfitPoints.getProfitPointWithStopPrice 1 shortPosition |> should equal 27.5m
    ProfitPoints.getProfitPointWithStopPrice 2 shortPosition |> should equal 22.5m
    ProfitPoints.getProfitPointWithStopPrice 3 shortPosition |> should equal 17.5m
    ProfitPoints.getProfitPointWithStopPrice 4 shortPosition |> should equal 12.5m
    ProfitPoints.getProfitPointWithStopPrice 5 shortPosition |> should equal 7.5m
    
[<Fact>]
let ``Percent levels for short position works`` () =
    ProfitPoints.getProfitPointWithPercentGain 1 0.05m shortPosition |> should equal 30.875m
    ProfitPoints.getProfitPointWithPercentGain 2 0.05m shortPosition |> should equal 29.25m
    ProfitPoints.getProfitPointWithPercentGain 3 0.05m shortPosition |> should equal 27.625m
    ProfitPoints.getProfitPointWithPercentGain 4 0.05m shortPosition |> should equal 26.0m
    ProfitPoints.getProfitPointWithPercentGain 5 0.05m shortPosition |> should equal 24.375m
    
[<Fact>]
let ``Get profit points list for long works`` () =
    let profitPoints = ProfitPoints.getProfitPointsWithStopPrice 4 longPosition
    profitPoints |> should haveLength 4
    profitPoints |> should equal [37.5m; 42.5m; 47.5m; 52.5m]
    
[<Fact>]
let ``Get profit points list for short works`` () =
    let profitPoints = ProfitPoints.getProfitPointsWithStopPrice 4 shortPosition
    profitPoints |> should haveLength 4
    profitPoints |> should equal [27.5m; 22.5m; 17.5m; 12.5m]