module studiestests.TypeTests

open System
open Xunit
open FsUnit
open studies.Types
open testutils

[<Fact>]
let ``loading signals csv works`` () =
    
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsPath);
    
    signals.Rows |> Seq.length |> should equal 48
    
[<Fact>]
let ``loading transformed csv works`` () =
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsTransformedPath);
    signals.Rows |> Seq.length |> should equal 32
    
[<Fact>]
let ``Verifying test data should pass`` () =
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsTransformedPath);
    Signal.verifyRecords signals 10 |> ignore
    
[<Fact>]
let ``verifyRecords throws exception when there are no records``() =
    let emptySignal = new Signal()
    (fun () -> Signal.verifyRecords emptySignal 100 |> ignore)
    |> should (throwWithMessage "0 is not enough records, expecting at least 100") typeof<Exception>
    
[<Fact>]
let ``Verification fails if ticker is blank``() =
    let row = studies.Types.Signal.Row(date="2023-01-01", ticker="", screenerid=1)
    let signals = new Signal([row])
    (fun () -> Signal.verifyRecords signals 1 |> ignore)
    |> should (throwWithMessage "ticker is blank for record 1, 2023-01-01") typeof<Exception>
    
[<Fact>]
let ``Verification fails if date is blank``() =
    let row = studies.Types.Signal.Row(date="", ticker=TestDataGenerator.NET.Value, screenerid=1)
    let signals = new Signal([row])
    (fun () -> Signal.verifyRecords signals 1 |> ignore)
    |> should (throwWithMessage "date is invalid for record 1, NET") typeof<Exception>
    
[<Fact>]
let ``Verification fails if screenerid is blank``() =
    let row = studies.Types.Signal.Row(date="2023-01-01", ticker=TestDataGenerator.NET.Value, screenerid=0)
    let signals = new Signal([row])
    (fun () -> Signal.verifyRecords signals 1 |> ignore)
    |> should (throwWithMessage "screenerid is blank for record NET, 2023-01-01") typeof<Exception>
    
[<Fact>]
let ``Describe records for Signal works``() =
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsPath)
    signals.Rows |> Seq.map Input |> Unified.describeRecords