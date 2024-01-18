module coretests.fs.Stocks.Services.GapAnalysisTests

open Xunit
open core.fs.Services
open core.fs.Services.Analysis
open testutils
open FsUnit

let gaps =
    GapAnalysis.detectGaps
        (TestDataGenerator.PriceBars(TestDataGenerator.NET))
        SingleBarPriceAnalysis.SingleBarAnalysisConstants.NumberOfDaysForRecentAnalysis


[<Fact>]
let ``Number of Gaps Detected Matches`` () =
    gaps.Length |> should equal 11

[<Fact>]
let ``Gap PercentChange Correct`` () =
    System.Math.Round(gaps.[0].PercentChange, 3) |> should equal 0.052m

[<Fact>]
let ``Gap RelativeVolume Correct`` () =
    gaps.[0].RelativeVolume |> should equal 0.94m

[<Fact>]
let ``Gap GapType Correct`` () =
    gaps.[0].Type |> should equal GapAnalysis.GapType.Up

[<Fact(Skip = "Need to review if the logic is correct")>]
let ``ClosedQuickly Matches`` () =
    gaps |> Array.filter (fun g -> g.ClosedQuickly) |> Array.length |> should equal 7

[<Fact(Skip = "Need to review if the logic is correct")>]
let ``Open Matches`` () =
    gaps |> Array.filter (fun g -> g.Open) |> Array.length |> should equal 4