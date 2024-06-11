module studiestests.TypeTests

open System
open Xunit
open FsUnit
open studies.Trading
open studies.DataHelpers
open studies.ScreenerStudy
open testutils

let round (number:decimal) = Math.Round(number, 4)

[<Fact>]
let ``loading signals csv works`` () =

    let signals = Signal.Load(StudiesTestData.SignalsPath);

    signals.Rows |> Seq.length |> should equal 48

[<Fact>]
let ``loading transformed csv works`` () =
    let signals = Signal.Load(StudiesTestData.SignalsTransformedPath);
    signals.Rows |> Seq.length |> should equal 32

[<Fact>]
let ``Verifying test data should pass`` () =
    let signals = Signal.Load(StudiesTestData.SignalsTransformedPath).Rows |> Seq.map SignalWrapper |> Seq.cast<ISignal>;
    verifySignals signals 10 |> ignore

[<Fact>]
let ``verifyRecords throws exception when there are no records``() =
    let emptySignal = []
    (fun () -> verifySignals emptySignal 100 |> ignore)
    |> should (throwWithMessage "0 is not enough records, expecting at least 100") typeof<Exception>

[<Fact>]
let ``Verification fails if ticker is blank``() =
    let row = Signal.Row(date="2023-01-01", ticker="", screenerid=1)
    let signals = row |> SignalWrapper :> ISignal |> Seq.singleton
    (fun () -> verifySignals signals 1 |> ignore)
    |> should (throwWithMessage "ticker is blank for record 1, 2023-01-01") typeof<Exception>

[<Fact>]
let ``Verification fails if date is blank``() =
    let row = Signal.Row(date="", ticker=TestDataGenerator.NET.Value, screenerid=1)
    let signals = row |> SignalWrapper :> ISignal |> Seq.singleton
    (fun () -> verifySignals signals 1 |> ignore)
    |> should (throwWithMessage "date is invalid for record 1, NET") typeof<Exception>

[<Fact>]
let ``Verification fails if screenerid is blank``() =
    let row = Signal.Row(date="2023-01-01", ticker=TestDataGenerator.NET.Value, screenerid=0)
    let signals = row |> SignalWrapper :> ISignal |> Seq.singleton
    (fun () -> verifySignals signals 1 |> ignore)
    |> should (throwWithMessage "screenerid is blank for record NET, 2023-01-01") typeof<Exception>

[<Fact>]
let ``Describe records for Signal works``() =
    let signals = Signal.Load(StudiesTestData.SignalsPath)
    signals.Rows |> Seq.map SignalWrapper |> Seq.cast<ISignal> |> describeSignals

let generateTradeOutcomes gains =
    gains
        |> List.map( fun g ->
            TradeOutcomeOutput.Row(
                    screenerid = (1 |> Some), date = "2022-08-08", ticker = "NET",
                    gap = (0m |> Some), sma20 = (0m |> Some), sma50 = (0m |> Some), sma150 = (0m |> Some), sma200 = (0m |> Some),
                    strategy = "B&H with trailing stop", longOrShort = "long",
                    opened = "2022-08-08", openPrice = 74.425m,
                    closed = "2022-08-17", closePrice = 74.51m,
                    percentGain = g,
                    numberOfDaysHeld = 9
                )
        )

[<Fact>]
let ``Creating trade outcomes works``() =

    let outcomes = [0.02m; 0.01m; 1m; -0.05m; -0.08m] |> generateTradeOutcomes

    let summary = studies.Trading.TradeSummary.create "B&H with trailing stop" outcomes

    summary.Gains |> Seq.map (fun g -> Math.Round(g, 4)) |> should equal [0.02m; 0.01m; 1m; -0.05m; -0.08m]
    summary.Losers |> should equal 2
    summary.NumberOfTrades |> should equal 5
    summary.Winners |> should equal 3
    summary.AvgLoss |> round |> should equal -0.065m
    summary.AvgWin |> round |> should equal 0.3433m
    summary.EV  |> round |> should equal 0.18m
    summary.WinPct |> round |> should equal 0.6m
    summary.AvgGainLoss |> round |> should equal 5.2821m
    summary.StrategyName |> should equal "B&H with trailing stop"

[<Fact>]
let ``Trade outcomes with zero wins works`` () =
    [-0.05m] |> generateTradeOutcomes |> TradeSummary.create "B&H with trailing stop" |> ignore

[<Fact>]
let ``Trade outcomes with zero losses works`` () =
    [0.05m] |> generateTradeOutcomes |> TradeSummary.create "B&H with trailing stop" |> ignore
