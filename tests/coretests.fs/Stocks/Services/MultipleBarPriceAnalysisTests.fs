module coretests.fs.Stocks.Services.MultipleBarPriceAnalysisTests

open Xunit
open core.fs.Services
open core.fs.Services.Analysis
open testutils
open FsUnit

let outcomesWithSmallNumberOfBars = MultipleBarPriceAnalysis.run (TestDataGenerator.IncreasingPriceBars(10));
let rounded (decimals:int) (value:decimal) = System.Math.Round(value, decimals)

[<Fact>]
let ``Outcomes for small number of bars are not empty``() =
    Assert.NotEmpty(outcomesWithSmallNumberOfBars)

let firstOutcomeGeneric key (outcomes:AnalysisOutcome list) = outcomes |> Seq.find(fun x -> x.Key = key) |> _.Value

let outcomes = MultipleBarPriceAnalysis.run (TestDataGenerator.PriceBars(TestDataGenerator.NET))

let private firstOutcome key = firstOutcomeGeneric key outcomes

[<Fact>]
let ``Outcomes for large number of bars are not empty``() =
    Assert.NotEmpty(outcomes)

[<Fact>]
let ``Percent above low outcome correct``() =
    Assert.Equal(0.3m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentAboveLow, 2)

[<Fact>]
let ``Percent below high outcome correct``() =
    Assert.Equal(0.77m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentBelowHigh, 2)

[<Fact>]
let ``Percent change average outcome correct``() =
    Assert.Equal(-0.0016m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentChangeAverage, 4)

[<Fact>]
let ``Percent change standard deviation outcome correct``() =
    Assert.Equal(0.0662m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PercentChangeStandardDeviation, 4)

[<Fact>]
let ``Lowest price outcome correct``() =
    Assert.Equal(37.84m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPrice, 2)

[<Fact>]
let ``Lowest price days ago outcome correct``() =
    Assert.True(firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPriceDaysAgo > 345m) // it keeps on increasing as time goes by and test data's date is static

[<Fact>]
let ``Highest price outcome correct``() =
    Assert.Equal(217.25m, firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.HighestPrice, 2)

[<Fact>]
let ``Highest price days ago outcome correct``() =
    let highestDaysAgo = firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.HighestPriceDaysAgo
    let lowestDaysAgo = firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.LowestPriceDaysAgo
    highestDaysAgo > lowestDaysAgo |> should be True

[<Fact>]
let ``Current price outcome correct``() =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.CurrentPrice |> should equal 49.14m
    
[<Fact>]
let ``Earliest price outcome correct``() =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EarliestPrice |> should equal 75.08m

[<Fact>]
let ``Gain should be correct()`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.Gain |> rounded 2 |> should equal -0.35m

[<Fact>]
let ``Average true range should be correct`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageTrueRange |> rounded 2 |> should equal 3.98m
    
[<Fact>]
let ``Green streak is positive`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.GreenStreak |> should equal 1
    
[<Fact>]
let ``EMA20 above SMA50 is negative and matches`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EMA20AboveSMA50Bars |> should equal -48m
    
[<Fact>]
let ``Price above EMA20 bars is positive and matches`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAboveEMA20Bars |> should equal 1m

[<Fact>]
let ``SMA50 above SMA200 bars is positive and matches`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA50AboveSMA200Bars |> should equal -205m
    
[<Fact>]
let ``On Balance Volume is present and matches`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.OnBalanceVolume |> should equal 41298923m
    
[<Fact>]
let ``Average True Range Percentage is present and matches`` () =
    firstOutcome MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.AverageTrueRangePercentage |> rounded 2 |> should equal 0.08m
