module coretests.fs.Alerts.AlertsHandlerTests

open Xunit
open core.fs.Alerts
open FsUnit
open Moq
open core.fs.Adapters.SMS
open core.fs.Adapters.Logging
open core.fs.Adapters.Storage
open core.fs.Adapters.Email
open core.fs.Adapters.Brokerage
open core.fs.Alerts.MonitoringServices
open core.Shared

[<Fact>]
let ``Available monitors excludes OBV price trend`` () =
    let accountStorage = Mock.Of<IAccountStorage>()
    let alertEmailService =
        AlertEmailService(
            accountStorage,
            Mock.Of<IBlobStorage>(),
            StockAlertContainer(),
            Mock.Of<IEmailService>(),
            Mock.Of<ILogger>(),
            Mock.Of<IMarketHours>())

    let handler = Handler(
        StockAlertContainer(),
        Mock.Of<ISMSClient>(),
        alertEmailService,
        Mock.Of<ILogger>(),
        accountStorage)

    let monitors = handler.Handle (QueryAvailableMonitors()) |> Seq.toList

    monitors |> should haveLength 1
    monitors |> Seq.map _.name |> should equal [Constants.MonitorNamePattern]
    monitors |> Seq.map _.tag |> should equal [Constants.MonitorTagPattern]
