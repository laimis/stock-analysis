module coretests.fs.Alerts.AlertsHandlerTests

open Xunit
open core.fs.Alerts
open FsUnit

[<Fact>]
let ``Available monitors excludes OBV price trend`` () =
    let handler = Handler(
        Unchecked.defaultof<_>,
        Unchecked.defaultof<_>,
        Unchecked.defaultof<_>,
        Unchecked.defaultof<_>,
        Unchecked.defaultof<_>)

    let monitors = handler.Handle (QueryAvailableMonitors()) |> Seq.toList

    monitors |> should haveLength 1
    monitors |> Seq.map _.name |> should equal [Constants.MonitorNamePattern]
    monitors |> Seq.map _.tag |> should equal [Constants.MonitorTagPattern]
