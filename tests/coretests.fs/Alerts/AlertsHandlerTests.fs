module coretests.fs.Alerts.AlertsHandlerTests

open Xunit
open core.fs.Alerts
open FsUnit
open Moq
open core.fs.Adapters.SMS
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage

[<Fact>]
let ``Available monitors excludes OBV price trend`` () =
    let handler = Handler(
        StockAlertContainer(),
        Mock.Of<ISMSClient>(),
        Unchecked.defaultof<_>,
        Mock.Of<ILogger>(),
        Mock.Of<IAccountStorage>())

    let monitors = handler.Handle (QueryAvailableMonitors()) |> Seq.toList

    monitors |> should haveLength 1
    monitors |> Seq.map _.name |> should equal [Constants.MonitorNamePattern]
    monitors |> Seq.map _.tag |> should equal [Constants.MonitorTagPattern]
