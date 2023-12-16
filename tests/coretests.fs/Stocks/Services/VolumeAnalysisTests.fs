module coretests.fs.Stocks.Services.VolumeAnalysisTests

open Xunit
open core.fs.Services
open coretests.testdata
open FsUnit

let outcomes = TestDataGenerator.IncreasingPriceBars(300)
                |> MultipleBarPriceAnalysis.VolumeAnalysis.generate

[<Fact>]
let ``VolumeAnalysis Adds AllOutcomes`` () =
    outcomes |> Seq.length |> should equal 1
    
[<Fact>]
let ``AverageVolume PresentAndValid`` () =
    outcomes |> Seq.head |> _.Value |> should equal 269m
    outcomes |> Seq.head |> _.Key |> should equal MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageVolume