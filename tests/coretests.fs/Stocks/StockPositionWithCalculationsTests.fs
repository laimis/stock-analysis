module coretests.fs.Stocks.StockPositionWithCalculationsTests

open System
open FsUnit
open Xunit
open core.fs.Stocks
open coretests.testdata


let position =
    StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21"))
    |> StockPositionWithCalculations
    
let positionWithStop =
    StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.setStop (Some 28m) (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21"))
    |> StockPositionWithCalculations
    

[<Fact>]
let ``LastBuyPrice is accurate`` () =
    position.LastBuyPrice |> should equal 35m

[<Fact>]
let ``LastSellPrice is accurate`` () =
    position.LastSellPrice |> should equal 37m

[<Fact>]
let ``RR is accurate`` () =
    Assert.Equal(1.33m, positionWithStop.RR, 2);

[<Fact>]
let ``GainPct is accurate`` () =
     Assert.Equal(0.185m, position.GainPct, 2);

[<Fact>]
let ``First Buy Cost Accurate`` () =
    position.FirstBuyPrice |> should equal 30m
    
[<Fact>]
let ``Completed position cost per share accurate`` () =
    position.CompletedPositionCostPerShare |> should equal 32.5m


[<Fact>]
let ``Completed position number of shares accurate`` () =
    position.CompletedPositionShares |> should equal 20m

[<Fact>]
let ``Risked amount is accurate`` () =
    positionWithStop.RiskedAmount.Value |> should equal 90m
    
[<Fact>]
let ``Cost at risk based on stop price is accurate`` () =
    let position =
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
        |> StockPosition.setStop (Some 20m) (DateTimeOffset.Parse("2020-01-25"))
        
    position |> StockPositionWithCalculations |> _.CostAtRiskBasedOnStopPrice.Value |> should equal 250m
    
    // now sell some and risk should be reduced
    let afterSell =
        position
        |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
        |> StockPositionWithCalculations
    
    afterSell.CostAtRiskBasedOnStopPrice.Value |> should equal 150m
    
[<Fact>]
let ``Average buy cost per share is accurate``() =
    position.AverageBuyCostPerShare |> should equal 32.5m
    
[<Fact>]
let ``Days held is accurate`` () =
    Math.Abs(57 - position.DaysHeld) |> should be (lessThanOrEqualTo 1)
    
[<Fact>]
let ``Cost is accurate`` () =
    let position =
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
        |> StockPositionWithCalculations
        
    position.Cost |> should equal 650m
    
[<Fact>]
let ``Set stop sets first stop`` () =
    let position =
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.setStop (Some 28m) (DateTimeOffset.Parse("2020-01-25"))
        |> StockPosition.setStop (Some 29m) (DateTimeOffset.Parse("2020-01-25"))
        |> StockPositionWithCalculations
        
    position.FirstStop.Value |> should equal 28m
    
[<Fact>]
let ``Profit calculations are accurate``() =
    position.Profit |> should equal 120m

[<Fact>]
let ``Position closed date is accurate``() =    
    position.Closed.Value |> should equal (DateTimeOffset.Parse("2020-03-21"))
    
[<Fact>]
let ``Ticker is set``() =
    position.Ticker |> should equal TestDataGenerator.NET