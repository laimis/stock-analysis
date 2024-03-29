module coretests.fs.Stocks.Services.MovingAveragesAnalysisTests

open Xunit
open core.fs.Services
open core.fs.Services.Analysis
open testutils
open FsUnit

let outcomes = TestDataGenerator.IncreasingPriceBars(260)
                |> MultipleBarPriceAnalysis.MovingAveragesAnalysis.generate

let assertOutcomeExistsAndValueMatches key value =
    let v = MultipleBarPriceAnalysisTests.firstOutcomeGeneric key outcomes
    v |> should equal value

[<Fact>]
let ``MovingAveragesAnalysis Adds AllOutcomes`` () =
    outcomes
    |> Seq.length
    |> should equal 8
    
[<Fact>]
let ``EMA20 present and valid`` ()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.ExponentialMovingAverage 20) 249.5m
    
[<Fact>]
let ``SMA20 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage 20) 248.5m

[<Fact>]
let ``SMA50 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage 50) 233.5m

[<Fact>]
let ``SMA150 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage 150) 183.5m

[<Fact>]
let ``SMA200 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SimpleMovingAverage 200) 158.5m

[<Fact>]
let ``SMA20 above SMA50``()
    = assertOutcomeExistsAndValueMatches MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EMA20AboveSMA50Bars 210m
    
[<Fact>]
let ``SMA50 above SMA200``()
    = assertOutcomeExistsAndValueMatches MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA50AboveSMA200Bars 60m

[<Fact>]
let ``Price above EMA20Bars``()
    = assertOutcomeExistsAndValueMatches MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAboveEMA20Bars 241m
    
[<Fact>]
let ``EMA20 above SMA50 positive``()
    = outcomes
        |> Seq.find (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.EMA20AboveSMA50Bars)
        |> fun o -> o.OutcomeType |> should equal OutcomeType.Positive
