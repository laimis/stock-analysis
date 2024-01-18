module coretests.fs.Stocks.Services.PositionAnalysisTests

open Xunit
open core.Shared
open core.fs.Adapters.Brokerage
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Stocks
open testutils
open FsUnit

let createTestData() =
    
    let ticker = Ticker("SHEL")
    
    let bars = ticker |> TestDataGenerator.PriceBars
    
    let position =
        StockPosition.openLong ticker bars.First.Date
        |> StockPosition.buy 10m 100m (bars.First.Date)
        |> StockPosition.setStop (Some 90m) (bars.First.Date)
        
    let orders = [| Order(Ticker = Some(ticker), Price = 100m, Type = "SELL") |]
    
    (position, bars, orders)
    
let generateEvaluationsFromTestData() =
    
    let position, bars, orders = createTestData()
    
    let outcomes = [
        {
            outcomes = PositionAnalysis.generate (position|> StockPositionWithCalculations) bars orders
            ticker = position.Ticker
        }
    ]
    
    PositionAnalysis.evaluate outcomes


[<Fact>]
let ``Position with no strategy label sets appropriate key value``() =
    
    let position, bars, orders = createTestData()
    
    let outcomes = PositionAnalysis.generate (position|> StockPositionWithCalculations) bars orders
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PositionAnalysisKeys.StrategyLabel && o.Value = 0m) |> should equal true

[<Fact>]
let ``Evaluate with no strategy, selects missing strategy ticker``() =
    generateEvaluationsFromTestData()
    |> Seq.filter (fun e -> e.SortColumn = PositionAnalysis.PositionAnalysisKeys.StrategyLabel)
    |> Seq.head
    |> _.MatchingTickers
    |> should not' Empty
    
[<Fact>]
let ``Daily PL Correct`` () =
    
    let position, bars, _ = createTestData()
    
    let midPointInBars = bars.Length / 2
    
    let sold =
        position
        |> StockPosition.sell position.NumberOfShares bars.Bars[midPointInBars].Close bars.Bars[midPointInBars].Date
        |> StockPositionWithCalculations
    
    let dailyPlAndGain = PositionAnalysis.dailyPLAndGain bars sold
    
    dailyPlAndGain |> fst |> _.Data.Count |> should equal (midPointInBars + 1)
    dailyPlAndGain |> snd |> _.Data.Count |> should equal (midPointInBars + 1)
    
[<Fact>]
let ``Stop price set, percent to stop matches stop level``() =
    
    let position, bars, _ = createTestData()
    
    let setStopAndReturnOutcomes stopPrice =
        let withStopBelow =
            position
            |> StockPosition.setStop (Some stopPrice) (position.ShareTransactions[position.ShareTransactions.Length-1].Date)
            |> StockPositionWithCalculations
        
        PositionAnalysis.generate withStopBelow bars [||]
        
    let outcomes = setStopAndReturnOutcomes (bars.Last.Close - 2m)
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PositionAnalysisKeys.PercentToStopLoss && o.Value < 0m) |> should equal true
    
    let outcomes = setStopAndReturnOutcomes (bars.Last.Close + 2m)
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PositionAnalysisKeys.PercentToStopLoss && o.Value > 0m) |> should equal true
    
[<Fact>]
let ``Stop price set, for short position, percent to stop matches stop level`` () =
    
    let _, bars, _ = createTestData()
    
    let position =
        StockPosition.openShort (Ticker("SHEL")) bars.First.Date
        |> StockPosition.sell 10m 50m bars.First.Date
        
    let setStopAndReturnOutcomes stopPrice =
        let withStopBelow =
            position
            |> StockPosition.setStop (Some stopPrice) (position.ShareTransactions[position.ShareTransactions.Length-1].Date)
            |> StockPositionWithCalculations
        
        PositionAnalysis.generate withStopBelow bars [||]
        
    let outcomes = setStopAndReturnOutcomes (bars.Last.Close + 2m)
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PositionAnalysisKeys.PercentToStopLoss && o.Value < 0m) |> should equal true
    
    let outcomes = setStopAndReturnOutcomes (bars.Last.Close - 2m)
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PositionAnalysisKeys.PercentToStopLoss && o.Value > 0m) |> should equal true