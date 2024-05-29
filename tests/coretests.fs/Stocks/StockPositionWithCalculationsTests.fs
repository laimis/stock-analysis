module coretests.fs.Stocks.StockPositionWithCalculationsTests

open System
open FsUnit
open Xunit
open core.fs.Stocks
open testutils


let ticker = TestDataGenerator.NET

[<Fact>]
let position =
    StockPosition.openLong ticker (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21"))
    |> StockPositionWithCalculations
    
let positionWithStop =
    StockPosition.openLong ticker (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.setStop (Some 28m) (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-03-21"))
    |> StockPositionWithCalculations
    
let shortPosition =
    StockPosition.openShort ticker (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-01-23"))
    |> StockPosition.sell 10m 37m (DateTimeOffset.Parse("2020-01-25"))
    |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-02-25"))
    |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-03-21"))
    |> StockPositionWithCalculations
    
[<Fact>]
let ``ClosePrice is accurate`` () =
    position.ClosePrice |> Option.get |> should equal 37m

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

// generate the same tests as above but for short position
[<Fact>]
let ``Short Close Price is accurate`` () =
    shortPosition.ClosePrice |> Option.get |> should equal 30m
    
[<Fact>]
let ``Short GainPct is accurate`` () =
    Assert.Equal(0.18m, shortPosition.GainPct, 2)
    
[<Fact>]
let ``Short First Buy Cost Accurate`` () =
    shortPosition.FirstBuyPrice |> should equal 35m
    
[<Fact>]
let ``Short Completed position cost per share accurate`` () =
    shortPosition.CompletedPositionCostPerShare |> should equal 38.5m
    
[<Fact>]
let ``Short Completed position number of shares accurate`` () =
    shortPosition.CompletedPositionShares |> should equal 20m
    
[<Fact>]
let ``Short Average buy cost per share represents sell side``() =
    shortPosition.AverageBuyCostPerShare |> should equal 38.5m
    
[<Fact>]
let ``Short Days held is accurate`` () =
    Math.Abs(57 - shortPosition.DaysHeld) |> should be (lessThanOrEqualTo 1)

[<Fact>]
let ``Short position profit is accurate`` () =
    shortPosition.Profit |> should equal 120m
    
[<Fact>]
let ``Short Cost is accurate`` () =
    let position =
        StockPosition.openShort ticker (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.sell 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.sell 10m 35m (DateTimeOffset.Parse("2020-01-25"))
        |> StockPositionWithCalculations
        
    position.Cost |> should equal -650m

[<Fact>]
let ``Cost at risk based on stop price is accurate`` () =
    let position =
        StockPosition.openLong ticker (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
        |> StockPosition.setStop (Some 20m) (DateTimeOffset.Parse("2020-01-25"))
        
    position |> StockPositionWithCalculations |> _.CostAtRiskBasedOnStopPrice.Value |> should equal 250m
    
    // now sell some and risk should be reduced
    let afterSell =
        position
        |> StockPosition.sell 10m 40m (DateTimeOffset.Parse("2020-02-25"))
    
    afterSell |> StockPositionWithCalculations |> _.CostAtRiskBasedOnStopPrice.Value |> should equal 150m
    
    // now move the stop and risk should be reduced to zero
    let afterStopMove =
        afterSell
        |> StockPosition.setStop (Some 40m) (DateTimeOffset.Parse("2020-02-25"))
        |> StockPositionWithCalculations
        
    afterStopMove.CostAtRiskBasedOnStopPrice.Value |> should equal 0m
    
[<Fact>]
let ``Average buy cost per share is accurate``() =
    position.AverageBuyCostPerShare |> should equal 32.5m
    
[<Fact>]
let ``Days held is accurate`` () =
    Math.Abs(57 - position.DaysHeld) |> should be (lessThanOrEqualTo 1)
    
[<Fact>]
let ``Cost is accurate`` () =
    let position =
        StockPosition.openLong ticker (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 35m (DateTimeOffset.Parse("2020-01-25"))
        |> StockPositionWithCalculations
        
    position.Cost |> should equal 650m
    
[<Fact>]
let ``Set stop sets first stop`` () =
    let position =
        StockPosition.openLong ticker (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.buy 10m 30m (DateTimeOffset.Parse("2020-01-23"))
        |> StockPosition.setStop (Some 28m) (DateTimeOffset.Parse("2020-01-25"))
        |> StockPosition.setStop (Some 29m) (DateTimeOffset.Parse("2020-01-25"))
        |> StockPositionWithCalculations
        
    position.FirstStop().Value |> should equal 28m
    
[<Fact>]
let ``Profit calculations are accurate``() =
    position.Profit |> should equal 120m

[<Fact>]
let ``Position closed date is accurate``() =    
    position.Closed.Value |> should equal (DateTimeOffset.Parse("2020-03-21"))
    
[<Fact>]
let ``Ticker is set``() =
    position.Ticker |> should equal ticker
    
   
[<Fact>]
let ``Multiple buys average cost correct`` () =
    let purchase1 =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
    
    let withCalculation = purchase1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 7.5m
    withCalculation.Cost |> should equal 15m
    
    let sell1 = StockPosition.sell 1m 6m DateTimeOffset.UtcNow purchase1
    let withCalculation = sell1 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 10m
    
    let purchase2 = StockPosition.buy 1m 10m DateTimeOffset.UtcNow sell1
    let withCalculation = purchase2 |> StockPositionWithCalculations
    withCalculation.AverageCostPerShare |> should equal 10m
    withCalculation.Cost |> should equal 20m
    
    let sell2 = StockPosition.sell 2m 10m DateTimeOffset.UtcNow purchase2
    sell2.IsClosed |> should equal true
    let withCalculation = sell2 |> StockPositionWithCalculations
    DateTimeOffset.UtcNow.Subtract(withCalculation.Closed.Value).TotalSeconds |> int |> should be (lessThan 1)
    withCalculation.DaysHeld |> should equal 0
    withCalculation.Profit |> should equal 1
    
    Assert.Equal(0.04m, withCalculation.GainPct, 2)
    
[<Fact>]
let ``Sell creates PL Transactions`` () =
    
    let stock =
        StockPosition.openLong ticker DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 5m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow 
        |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
        |> StockPosition.buy 1m 10m DateTimeOffset.UtcNow
        |> StockPosition.sell 2m 10m DateTimeOffset.UtcNow
        |> StockPositionWithCalculations
        
    let transactions = stock.PLTransactions
    
    transactions |> should haveLength 2
    
    transactions |> List.head |> _.Profit |> should equal 1m
    transactions |> List.last |> _.Profit |> should equal 0m
    
    
[<Fact>]
let ``Days held is correct`` () =
    
    let position =
        StockPosition.openLong ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 5m (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 10m (DateTimeOffset.UtcNow.AddDays(-2))
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 2
    
    let afterSell1 = position |> StockPosition.sell 1m 6m DateTimeOffset.UtcNow
    
    let calculated = afterSell1 |> StockPositionWithCalculations
    
    calculated.DaysHeld |> should equal 5
    calculated.DaysSinceLastTransaction |> should equal 0
    
[<Fact>]
let ``Percent to stop from cost is correct`` () =
    
    let position =
        StockPosition.openLong ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 5m (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.buy 1m 5m (DateTimeOffset.UtcNow.AddDays(-2))
        |> StockPosition.setStop (Some 4m) (DateTimeOffset.UtcNow.AddDays(-2))
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.PercentToStopFromCost |> should equal -0.2m
    
[<Fact>]
let ``Losing short position, profit calculation is accurate`` () =
    
    let position =
        StockPosition.openShort ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.sell 1m 5m (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.sell 1m 5m (DateTimeOffset.UtcNow.AddDays(-2))
        |> StockPosition.buy 1m 6m (DateTimeOffset.UtcNow.AddDays(-2))
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.Profit |> should equal -1m
    calculated.PLTransactions |> should haveLength 1
    
    let transaction = calculated.PLTransactions |> List.head
    
    transaction.Profit |> should equal -1m
    
    
[<Fact>]
let ``Cost at risk based on stop is accurate for short positions``() =
    
    let position =
        StockPosition.openShort ticker (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.sell 2m 5m (DateTimeOffset.UtcNow.AddDays(-5))
        |> StockPosition.setStop (Some 7m) (DateTimeOffset.UtcNow.AddDays(-2))
        
    let calculated = position |> StockPositionWithCalculations
    
    calculated.CostAtRiskBasedOnStopPrice.Value |> should equal 4m
    
    let afterSell = position |> StockPosition.buy 1m 4m DateTimeOffset.UtcNow
    
    let calculated = afterSell |> StockPositionWithCalculations
    
    calculated.CostAtRiskBasedOnStopPrice.Value |> should equal 2m
    
    // now move the stop and risk should be reduced to zero
    let afterStopMove =
        afterSell
        |> StockPosition.setStop (Some 5m) (DateTimeOffset.UtcNow)
        |> StockPositionWithCalculations
        
    afterStopMove.CostAtRiskBasedOnStopPrice.Value |> should equal 0m
