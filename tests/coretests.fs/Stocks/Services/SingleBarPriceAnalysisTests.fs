module coretests.fs.Stocks.Services.SingleBarPriceAnalysisTests

open Xunit
open core.Shared
open core.fs.Services.Analysis
open testutils
open FsUnit

let generate ticker =
    Ticker(ticker) |> TestDataGenerator.PriceBars |> SingleBarPriceAnalysis.run

// NOTE: this tests single bar analysis with a feed of prices that do not change
// to make sure analysis still runs and does not breakdown with various
// exceptions related to stddev and other stats being zero
[<Fact>]
let ``SingleBarPriceAnalysisWithPricesNotChangingTests`` () =
    "SWCH"
    |> generate
    |> should not' Empty

[<Fact>]
let ``Detects new high`` () =
    "SHEL"
    |> generate
    |> MultipleBarPriceAnalysisTests.firstOutcomeGeneric SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh
    |> should equal 1m

[<Fact>]
let ``When no new high, does not include new high`` () =
    "NET"
    |> generate
    |> MultipleBarPriceAnalysisTests.firstOutcomeGeneric SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh
    |> should equal 0m

let outcomes = "NET" |> generate

let firstOutcome key =
    MultipleBarPriceAnalysisTests.firstOutcomeGeneric key outcomes

[<Fact>]
let ``NET outcomes are not empty``() =
    outcomes |> should not' Empty
    
[<Fact>]
let ``Volume is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.Volume |> firstOutcome |> should equal 6933219m
    
[<Fact>]
let ``RelativeVolume is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.RelativeVolume |> firstOutcome |> should equal 1.34m
    
[<Fact>]
let ``Open is correct``() =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.Open |> firstOutcome |> should equal 43.95m
    
[<Fact>]
let ``Close is correct``() =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.Close |> firstOutcome |> should equal 49.14m
    
[<Fact>]
let ``Closing range is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.ClosingRange |> firstOutcome |> should equal 1m
    
[<Fact>]
let ``Percent change is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.PercentChange |> firstOutcome |> MultipleBarPriceAnalysisTests.rounded 2 |> should equal 0.10m
    
[<Fact>]
let ``SigmaRatio is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.SigmaRatio |> firstOutcome |> should equal 1.62m
    
[<Fact>]
let ``GapPercentage is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.GapPercentage |> firstOutcome |> should equal 0m
    
[<Fact>]
let ``NewLow is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewLow |> firstOutcome |> should equal 0m
    
[<Fact>]
let ``NewHigh is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.NewHigh |> firstOutcome |> should equal 0m
    
[<Fact>]
let ``SMA20Above50Bars is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.EMA20AboveSMA50Bars |> firstOutcome |> should equal -48m
    
[<Fact>]
let ``SMA50Above200Bars is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.SMA50AboveSMA200Bars |> firstOutcome |> should equal -205m
    
[<Fact>]
let ``PriceAbove200SMA is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.PriceAboveSMA200 |> firstOutcome |> MultipleBarPriceAnalysisTests.rounded 4 |> should equal -0.2918m
    
[<Fact>]
let ``TrueRange is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.TrueRange |> firstOutcome |> should equal 6m
    
[<Fact>]
let ``DollarChange is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.DollarChange |> firstOutcome |> should equal 4.53m
    
[<Fact>]
let ``PercentDifferenceFromLow is correct`` () =
    SingleBarPriceAnalysis.SingleBarOutcomeKeys.DollarChangeVsATR |> firstOutcome |> MultipleBarPriceAnalysisTests.rounded 2 |> should equal 1.14m

