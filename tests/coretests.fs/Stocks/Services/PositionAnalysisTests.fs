module coretests.fs.Stocks.Services.PositionAnalysisTests

open Xunit
open core.Shared
open core.fs.Services
open core.fs.Services.Analysis
open core.fs.Shared.Adapters.Brokerage
open core.fs.Shared.Domain
open coretests.testdata
open FsUnit

let createTestData() =
    
    let ticker = Ticker("SHEL")
    
    let bars = ticker |> TestDataGenerator.PriceBars
    
    let position =
        StockPosition.openLong ticker bars.First.Date
        |> StockPosition.buy 10m 100m (bars.First.Date) None
        
    let orders = [| Order(Ticker = Some(ticker), Price = 100m, Type = "SELL") |]
    
    (position, bars, orders)


[<Fact>]
let ``Position with no strategy label sets appropriate key value``() =
    
    let position, bars, orders = createTestData()
    
    let outcomes = PositionAnalysis.generate (position|> StockPositionWithCalculations) bars orders
    
    outcomes |> Seq.exists (fun o -> o.Key = PositionAnalysis.PortfolioAnalysisKeys.StrategyLabel && o.Value = 0m) |> should equal true

[<Fact>]
let ``Evaluate with no strategy, selects missing strategy ticker``() =
    
    let (position, bars, orders) = createTestData()
    
    let outcomes = [
        {
            outcomes = PositionAnalysis.generate (position|> StockPositionWithCalculations) bars orders
            ticker = position.Ticker
        }
    ]
    
    let evaluations = PositionAnalysis.evaluate outcomes
    
    evaluations |> Seq.exists (fun e -> e.SortColumn = PositionAnalysis.PortfolioAnalysisKeys.StrategyLabel) |> should equal true

[<Fact>]
let ``Daily PL Correct`` () =
    
    let position, bars, _ = createTestData()
    
    let midPointInBars = bars.Length / 2
    
    let sold =
        position
        |> StockPosition.sell position.NumberOfShares bars.Bars.[midPointInBars].Close bars.Bars.[midPointInBars].Date None
        |> StockPositionWithCalculations
    
    let dailyPlAndGain = PositionAnalysis.dailyPLAndGain bars sold
    
    dailyPlAndGain |> fst |> _.Data.Count |> should equal (midPointInBars + 1)
    dailyPlAndGain |> snd |> _.Data.Count |> should equal (midPointInBars + 1)