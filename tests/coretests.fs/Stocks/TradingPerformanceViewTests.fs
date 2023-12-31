module coretests.fs.Stocks.TradingPerformanceViewTests

open System
open Xunit
open core.fs.Portfolio
open core.fs.Services.Trading
open core.fs.Stocks
open coretests.fs.Stocks.Services
open coretests.testdata
open FsUnit
    
let getClosedPositions() =
    
    [
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.buy 1m 100m (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.setStop (Some 95m) (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.sell 1m 110m DateTimeOffset.UtcNow
        
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.buy 1m 100m (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.setStop (Some 95m) (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.sell 1m 110m DateTimeOffset.UtcNow
        
        StockPosition.openLong TestDataGenerator.NET (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.buy 1m 100m (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.setStop (Some 95m) (DateTimeOffset.UtcNow.AddDays(-1))
        |> StockPosition.sell 1m 90m DateTimeOffset.UtcNow
        
    ]

let generateRandomSet (start:DateTimeOffset) minimumNumberOfTrades =
    
    let random = Random()
    let numberOfTrades = random.Next(minimumNumberOfTrades, minimumNumberOfTrades + 100)
    
    let closedPositions = 
        [0..numberOfTrades]
        |> List.map (fun _ ->
            let stock = TestDataGenerator.GenerateRandomTicker random
            let purchaseDate = start.AddDays(-numberOfTrades)
            let shares = random.Next(1, 100) |> decimal
            let price = random.Next(1, 1000) |> decimal
            let sellDate = purchaseDate.AddDays(1)
            let sellPrice = random.Next(1, 1000) |> decimal
            
            StockPosition.openLong stock purchaseDate
            |> StockPosition.buy shares price purchaseDate
            |> StockPosition.sell shares sellPrice sellDate
        )
    
    closedPositions


let performance = getClosedPositions() |> List.map StockPositionWithCalculations |> TradingPerformance.Create "All"

[<Fact>]
let TestTotal() = performance.NumberOfTrades |> should equal 3

[<Fact>]
let WinsCorrect() = performance.Wins |> should equal 2

[<Fact>]
let LossesCorrect() = performance.Losses |> should equal 1

[<Fact>]
let WinPctCorrect() = performance.WinPct |> MultipleBarPriceAnalysisTests.rounded 2 |> should equal 0.67m

[<Fact>]
let AvgWinAmountCorrect() = performance.AvgWinAmount |> should equal 10m

[<Fact>]
let MaxWinAmountCorrect() = performance.MaxWinAmount |> should equal 10m

[<Fact>]
let WinAvgReturnPctCorrect() = performance.WinAvgReturnPct |> should equal 0.10m

[<Fact>]
let WinMaxReturnPctCorrect() = performance.WinMaxReturnPct |> should equal 0.10m

[<Fact>]
let WinAvgDaysHeldCorrect() = performance.WinAvgDaysHeld |> should equal 1.0m

[<Fact>]
let LossAvgAmountCorrect() = performance.AvgLossAmount |> should equal -10m

[<Fact>]
let LossMaxAmountCorrect() = performance.MaxLossAmount |> should equal -10m

[<Fact>]
let LossAvgReturnPctCorrect() = performance.LossAvgReturnPct |> should equal -0.10m

[<Fact>]
let LossMaxReturnPctCorrect() = performance.LossMaxReturnPct |> should equal -0.10m

[<Fact>]
let LossAvgDaysHeldCorrect() = performance.LossAvgDaysHeld |> should equal 1.0m

[<Fact>]
let AvgDaysHeldCorrect() = performance.AverageDaysHeld |> should equal 1.0m

[<Fact>]
let EV_Correct() = performance.EV |> should equal 10.00m

[<Fact>]
let ReturnPctRatio_Correct() = performance.ReturnPctRatio |> should equal 1

[<Fact>]
let rrRatio_Correct() = performance.rrRatio |> should equal 1

[<Fact>]
let ProfitRatio_Correct() = performance.ProfitRatio |> should equal 1

[<Fact>]
let AvgReturnPct_Correct() =
    performance.AvgReturnPct |> MultipleBarPriceAnalysisTests.rounded 2 |> should equal 0.03m

let container =
    generateRandomSet DateTimeOffset.UtcNow 100 |> List.map StockPositionWithCalculations |> List.toArray |> TradingPerformanceContainerView

[<Fact>]
let NumberOfTrades_Correct() = container.Performances[0].NumberOfTrades |> should be (lessThan container.Performances[1].NumberOfTrades)

[<Fact(Skip = "Need to fix trends")>]
let YTDContainer_Profit_DaysAreSequential() = 
    let dates =
        container.Trends
        |> Array.find (fun c -> c.Label = "Profits")
        |> fun c -> c.Data |> Seq.map (_.Label)
    
    dates
    |> Seq.pairwise
    |> Seq.forall (fun (a, b) -> DateTimeOffset.Parse(a) < DateTimeOffset.Parse(b))
    |> should equal true
    
[<Fact(Skip = "Need to fix trends")>]
let YTDContainer_Profit_NoRepeatingDays() = 
    let dates = container.Trends |> Array.find (fun c -> c.Label = "Profits") |> fun c -> c.Data |> Seq.map (_.Label)
    
    let distinctDates = dates |> Seq.distinct
    
    dates |> Seq.length |> should equal (distinctDates |> Seq.length)