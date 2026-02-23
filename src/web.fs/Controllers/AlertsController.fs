namespace web.Controllers

open System
open System.Threading.Tasks
open Microsoft.AspNetCore.Mvc
open Microsoft.AspNetCore.Authorization
open core.fs.Adapters.Logging
open core.fs.Alerts
open web.Utils

[<CLIMutable>]
type CreateStockPriceAlertRequest = {
    Ticker: string
    PriceLevel: decimal
    AlertType: string
    Note: string
}

[<CLIMutable>]
type UpdateStockPriceAlertRequest = {
    PriceLevel: decimal
    AlertType: string
    Note: string
    State: string
}

[<CLIMutable>]
type CreateReminderRequest = {
    Date: string
    Message: string
    Ticker: string option
}

[<CLIMutable>]
type UpdateReminderRequest = {
    Date: string
    Message: string
    Ticker: string option
    State: string
}

[<ApiController>]
[<Authorize>]
[<Route("api/[controller]")>]
type AlertsController(handler: Handler) =
    inherit ControllerBase()

    [<AllowAnonymous>]
    [<Consumes("text/plain")>]
    [<HttpPost("sms")>]
    member this.Sms([<FromBody>] body: string) =
        handler.Handle({SendSMS.Body = body})

    [<AllowAnonymous>]
    [<HttpGet("sms/status")>]
    member this.SmsStatus() =
        this.OkOrError(handler.Handle(SMSStatus()))

    [<Authorize("admin")>]
    [<HttpPost("sms/on")>]
    member this.SmsOn() =
        handler.Handle(TurnSMSOn())

    [<Authorize("admin")>]
    [<HttpPost("sms/off")>]
    member this.SmsOff() =
        handler.Handle(TurnSMSOff())

    [<Authorize("admin")>]
    [<HttpPost("run")>]
    member this.Run() =
        this.OkOrError(handler.Handle(Run()))

    [<HttpGet>]
    member this.Index() =
        handler.Handle({QueryAlerts.UserId = this.User.Identifier()})

    [<HttpGet("monitors")>]
    member this.Monitors() =
        handler.Handle(QueryAvailableMonitors())

    [<HttpGet("emailJob")>]
    [<Authorize("admin")>]
    member this.EmailJob() =
        handler.Handle(RunEmailJob())

    [<HttpGet("triggerWeekly")>]
    [<Authorize("admin")>]
    member this.TriggerWeekly([<FromServices>] logger: ILogger, [<FromServices>] weeklyAlerts: MonitoringServices.WeeklyMonitoringService) =
        weeklyAlerts.Execute(true)

    [<HttpGet("triggerMaxProfit")>]
    [<Authorize("admin")>]
    member this.TriggerMaxProfit([<FromServices>] service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) =
        service.ReportOnMaxProfitBasedOnDaysHeld()

    [<HttpGet("triggerThirtyDayTransactions")>]
    [<Authorize("admin")>]
    member this.TriggerThirtyDayTransactions([<FromServices>] service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) =
        service.ReportOnThirtyDayTransactions()

    [<HttpGet("triggerDaily")>]
    [<Authorize("admin")>]
    member this.TriggerDaily([<FromServices>] dailyAlerts: MonitoringServices.PatternMonitoringService) = task {
        return! dailyAlerts.RunPatternMonitoring()
    }

    [<HttpGet("triggerPriceObvTrend")>]
    [<Authorize("admin")>]
    member this.TriggerPriceObvTrend([<FromServices>] priceObvTrendAlerts: MonitoringServices.PriceObvTrendMonitoringService) = task {
        return! priceObvTrendAlerts.Run()
    }

    [<HttpGet("triggerEmail")>]
    [<Authorize("admin")>]
    member this.TriggerEmail([<FromServices>] emailAlerts: MonitoringServices.AlertEmailService) = task {
        return! emailAlerts.Execute()
    }

    [<HttpGet("triggerAccountMonitoring")>]
    [<Authorize("admin")>]
    member this.TriggerAccountMonitoring([<FromServices>] accountMonitoring: core.fs.Brokerage.MonitoringServices.AccountMonitoringService) =
        accountMonitoring.RunAccountValueOrderAndTransactionSync()

    [<HttpGet("triggerAccountTransactionProcessing")>]
    [<Authorize("admin")>]
    member this.TriggerAccountTransactionProcessing([<FromServices>] accountMonitoring: core.fs.Brokerage.MonitoringServices.AccountMonitoringService) =
        accountMonitoring.RunTransactionProcessing()

    [<HttpGet("triggerPortfolioProcessing")>]
    [<Authorize("admin")>]
    member this.TriggerPortfolioProcessing([<FromServices>] service: core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService) =
        service.RecentlyClosedPositionUpdates()

    [<HttpGet("triggerOptionPricing")>]
    [<Authorize("admin")>]
    member this.TriggerOptionPricing([<FromServices>] optionPricing: core.fs.Options.MonitoringServices.PriceMonitoringService) =
        optionPricing.Run()

    [<HttpGet("triggerPriceAlerts")>]
    [<Authorize("admin")>]
    member this.TriggerPriceAlerts([<FromServices>] priceAlerts: MonitoringServices.PriceAlertMonitoringService) =
        priceAlerts.Execute()

    [<HttpGet("triggerNearTriggerPriceAlerts")>]
    [<Authorize("admin")>]
    member this.TriggerNearTriggerPriceAlerts([<FromServices>] nearTriggerAlerts: MonitoringServices.PriceAlertNearTriggerMonitoringService) =
        nearTriggerAlerts.Execute()

    [<HttpGet("triggerReminders")>]
    [<Authorize("admin")>]
    member this.TriggerReminders([<FromServices>] reminderService: MonitoringServices.ReminderMonitoringService) =
        reminderService.Execute()

    [<HttpGet("triggerSECFilingsSync")>]
    [<Authorize("admin")>]
    member this.TriggerSECFilingsSync([<FromServices>] secFilingsSyncService: SECFilingsMonitoring.SECFilingsSyncService) =
        secFilingsSyncService.Execute()

    [<HttpGet("triggerSECFilings")>]
    [<Authorize("admin")>]
    member this.TriggerSECFilings([<FromServices>] secFilingsService: SECFilingsMonitoring.SECFilingsMonitoringService) =
        secFilingsService.Execute()

    [<HttpGet("triggerSchedule13G")>]
    [<Authorize("admin")>]
    member this.TriggerSchedule13G([<FromServices>] service: secedgar.fs.Schedule13GProcessingService) =
        service.Execute()

    [<HttpGet("triggerForm144")>]
    [<Authorize("admin")>]
    member this.TriggerForm144([<FromServices>] service: secedgar.fs.Form144ProcessingService) =
        service.Execute()

    [<HttpGet("triggerSchedule13D")>]
    [<Authorize("admin")>]
    member this.TriggerSchedule13D([<FromServices>] service: secedgar.fs.Schedule13DProcessingService) =
        service.Execute()

    [<HttpGet("price")>]
    member this.GetStockPriceAlerts() =
        this.OkOrError(handler.Handle({GetStockPriceAlerts.UserId = this.User.Identifier()}))

    [<HttpPost("price")>]
    member this.CreateStockPriceAlert([<FromBody>] request: CreateStockPriceAlertRequest) =
        this.OkOrError(handler.Handle({
            CreateStockPriceAlert.UserId = this.User.Identifier()
            Ticker = request.Ticker
            PriceLevel = request.PriceLevel
            AlertType = request.AlertType
            Note = request.Note
        }))

    [<HttpPut("price/{alertId}")>]
    member this.UpdateStockPriceAlert([<FromRoute>] alertId: Guid, [<FromBody>] request: UpdateStockPriceAlertRequest) =
        this.OkOrError(handler.Handle({
            UpdateStockPriceAlert.UserId = this.User.Identifier()
            AlertId = alertId
            PriceLevel = request.PriceLevel
            AlertType = request.AlertType
            Note = request.Note
            State = request.State
        }))

    [<HttpPost("price/{alertId}/reset")>]
    member this.ResetStockPriceAlert([<FromRoute>] alertId: Guid) =
        this.OkOrError(handler.Handle({
            ResetStockPriceAlert.UserId = this.User.Identifier()
            AlertId = alertId
        }))

    [<HttpDelete("price/{alertId}")>]
    member this.DeleteStockPriceAlert([<FromRoute>] alertId: Guid) =
        this.OkOrError(handler.Handle({DeleteStockPriceAlert.UserId = this.User.Identifier(); AlertId = alertId}))

    [<HttpGet("reminders")>]
    member this.GetReminders() =
        this.OkOrError(handler.Handle({GetReminders.UserId = this.User.Identifier()}))

    [<HttpPost("reminders")>]
    member this.CreateReminder([<FromBody>] request: CreateReminderRequest) =
        this.OkOrError(handler.Handle({
            CreateReminder.UserId = this.User.Identifier()
            Date = request.Date
            Message = request.Message
            Ticker = request.Ticker
        }))

    [<HttpPut("reminders/{reminderId}")>]
    member this.UpdateReminder([<FromRoute>] reminderId: Guid, [<FromBody>] request: UpdateReminderRequest) =
        this.OkOrError(handler.Handle({
            UpdateReminder.UserId = this.User.Identifier()
            ReminderId = reminderId
            Date = request.Date
            Message = request.Message
            Ticker = request.Ticker
            State = request.State
        }))

    [<HttpDelete("reminders/{reminderId}")>]
    member this.DeleteReminder([<FromRoute>] reminderId: Guid) =
        this.OkOrError(handler.Handle({DeleteReminder.UserId = this.User.Identifier(); ReminderId = reminderId}))
