module coretests.fs.Stocks.Services.SMAAnalysisTests

open Xunit
open core.fs.Services
open core.fs.Services.Analysis
open testutils
open FsUnit

let outcomes = TestDataGenerator.IncreasingPriceBars(260)
                |> MultipleBarPriceAnalysis.SMAAnalysis.generate

let assertOutcomeExistsAndValueMatches key value =
    let v = MultipleBarPriceAnalysisTests.firstOutcomeGeneric key outcomes
    v |> should equal value

[<Fact>]
let ``SMAAnalysis Adds AllOutcomes`` () =
    outcomes
    |> Seq.length
    |> should equal 6
    
[<Fact>]
let ``SMA20 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(20)) 248.5m

[<Fact>]
let ``SMA50 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(50)) 233.5m

[<Fact>]
let ``SMA150 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(150)) 183.5m

[<Fact>]
let ``SMA200 present and valid``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA(200)) 158.5m

[<Fact>]
let ``SMA20 above SMA50``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA20Above50Days) 210m

[<Fact>]
let ``Price above 20SMA``()
    = assertOutcomeExistsAndValueMatches (MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.PriceAbove20SMADays) 240m

[<Fact>]
let ``SMA20 above SMA50 positive``()
    = outcomes
        |> Seq.find (fun o -> o.Key = MultipleBarPriceAnalysis.MultipleBarOutcomeKeys.SMA20Above50Days)
        |> fun o -> o.OutcomeType |> should equal OutcomeType.Positive