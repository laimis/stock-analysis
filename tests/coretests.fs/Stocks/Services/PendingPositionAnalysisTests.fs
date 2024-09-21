module coretests.fs.Stocks.Services.PendingPositionAnalysisTests

open System
open Xunit
open core.Shared
open core.Stocks
open core.fs.Services.PendingPositionAnalysis
open testutils
open FsUnit

let ticker = Ticker("SHEL")

let bars = ticker |> TestDataGenerator.PriceBars

let position = PendingStockPosition("some note", 10m, 100m, 90m, "channelbottom", ticker, Guid.NewGuid())

let outcomes = generate position.State bars

let rounded (value:decimal) = Math.Round(value, 2)

[<Fact>]
let ``Position has outcomes``() =
    outcomes.outcomes |> Seq.isEmpty |> should equal false
    
[<Fact>]
let ``Percent from bid is accurate``() =
    let percentFromPrice = outcomes.outcomes |> Seq.find (fun o -> o.Key = PendingPositionAnalysisKeys.PercentFromPrice)
    percentFromPrice.Value |> rounded |> should equal 0.62
    
[<Fact>]
let ``Position size is accurate``() =
    let positionSize = outcomes.outcomes |> Seq.find (fun o -> o.Key = PendingPositionAnalysisKeys.PositionSize)
    positionSize.Value |> should equal 1000m
    
[<Fact>]
let ``Risked amount is accurate``() =
    let riskedAmount = outcomes.outcomes |> Seq.find (fun o -> o.Key = PendingPositionAnalysisKeys.RiskedAmount)
    riskedAmount.Value |> should equal 100m
    
    
