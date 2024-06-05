module coretests.fs.Stocks.Services.TrendsTests

open Xunit
open FsUnit
open core.fs.Services.Trends
open testutils

let trends = TestDataGenerator.PriceBars(TestDataGenerator.NET) |> TrendCalculator.generate TestDataGenerator.NET TrendType.Ema20OverSma50
let rounded (precision:int) (value:decimal) = System.Math.Round(value, precision)

[<Fact>]
let ``trends length is correct``() =
    trends.Length |> should equal 12

[<Fact>]
let ``trends start value is correct``() =
    trends.StartDateStr |> should equal "2020-11-30"
    
[<Fact>]
let ``trends end value is correct``() =
    trends.EndDateStr |> should equal "2022-11-30"

[<Fact>]
let ``ticker is correct``() =
    trends.Ticker |> should equal TestDataGenerator.NET

[<Fact>]
let ``All trend bars stats are correct``() =
    
    let stats = trends.BarStatistics
    
    stats.count |> should equal 12
    stats.mean |> rounded 2 |> should equal 41.08
    stats.median |> should equal 45
    stats.max |> should equal 91
    stats.min |> should equal 1
    
[<Fact>]
let ``Up trend bars stats are correct``() =
    
    let stats = trends.UpBarStatistics
    
    stats.count |> should equal 6
    stats.mean |> rounded 2 |> should equal 44.83
    stats.median |> should equal 45
    stats.max |> should equal 91
    stats.min |> should equal 16
    
[<Fact>]
let ``Down trend bars stats are correct``() =
    
    let stats = trends.DownBarStatistics
    
    stats.count |> should equal 6
    stats.mean |> rounded 2 |> should equal 37.33
    stats.median |> should equal 47
    stats.max |> should equal 68
    stats.min |> should equal 1

[<Fact>]
let ``Current trend params are correct``() =
    trends.CurrentTrend.trendType |> should equal TrendType.Ema20OverSma50
    trends.CurrentTrend.direction |> should equal Down
    trends.CurrentTrend.start |> snd |> _.DateStr |> should equal "2022-09-23"
    trends.CurrentTrend.end_ |> snd |> _.DateStr |> should equal "2022-11-30"
    trends.CurrentTrend.GainPercent |> MultipleBarPriceAnalysisTests.rounded 3 |> should equal -0.097
    trends.CurrentTrend.MaxAge |> should equal 38
    
    // rank
    trends.BarRank trends.CurrentTrend |> fst |> should equal 3
    trends.GainRank trends.CurrentTrend |> fst |> should equal 4
    
    // rank obtained differently
    trends.CurrentTrendRankByBars |> should equal 3
    trends.CurrentTrendRankByGain |> should equal 4
