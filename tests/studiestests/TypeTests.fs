module Tests

open Xunit
open FsUnit
open testutils

[<Fact>]
let ``loading signals csv works`` () =
    
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsPath);
    
    signals.Rows |> Seq.length |> should equal 48
    
[<Fact>]
let ``loading transformed csv works`` () =
    
    let signals = studies.Types.Signal.Load(StudiesTestData.SignalsTransformedPath);
    
    signals.Rows |> Seq.length |> should equal 32