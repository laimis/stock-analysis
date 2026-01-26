using System.Collections.Generic;
using System.Threading.Tasks;
using core.fs.Adapters.Logging;
using core.fs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

#nullable enable

namespace web.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class AlertsController(Handler handler) : ControllerBase
{
    // TODO: this is exposed anonymously to allow trendview to ping this endpoint
    // and they don't support authorization. I should at the very least set up some sort of query
    // string key or something along those lines
    [AllowAnonymous]
    [Consumes("text/plain")]
    [HttpPost("sms")]
    public Task Sms([FromBody] string body) =>
        handler.Handle(new SendSMS(body));

    [AllowAnonymous]
    [HttpGet("sms/status")]
    public Task<ActionResult> SmsStatus() =>
        this.OkOrError(handler.Handle(new SMSStatus()));

    [Authorize("admin")]
    [HttpPost("sms/on")]
    public Task SmsOn() => handler.Handle(new TurnSMSOn());

    [Authorize("admin")]
    [HttpPost("sms/off")]
    public Task SmsOff() => handler.Handle(new TurnSMSOff());

    [Authorize("admin")]
    [HttpPost("run")]
    public ActionResult Run() => this.OkOrError(handler.Handle(new Run()));

    [HttpGet]
    public AlertsView Index() => handler.Handle(new QueryAlerts(User.Identifier()));

    [HttpGet("monitors")]
    public IEnumerable<object> Monitors() => handler.Handle(new QueryAvailableMonitors());
        
    [HttpGet("emailJob")]
    [Authorize("admin")]
    public Task EmailJob() => handler.Handle(new RunEmailJob());

    [HttpGet("triggerWeekly")]
    [Authorize("admin")]
    public Task TriggerWeekly(
        [FromServices] ILogger logger,
        [FromServices] MonitoringServices.WeeklyMonitoringService weeklyAlerts) => weeklyAlerts.Execute(true);

    [HttpGet("triggerMaxProfit")]
    [Authorize("admin")]
    public Task TriggerMaxProfit(
        [FromServices] core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService service) => service.ReportOnMaxProfitBasedOnDaysHeld();
    
    [HttpGet("triggerThirtyDayTransactions")]
    [Authorize("admin")]
    public Task TriggerThirtyDayTransactions(
        [FromServices] core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService service) => service.ReportOnThirtyDayTransactions();
        
    [HttpGet("triggerDaily")]
    [Authorize("admin")]
    public Task TriggerDaily([FromServices] MonitoringServices.PatternMonitoringService dailyAlerts)
    {
        dailyAlerts.RunPatternMonitoring();
        return Task.CompletedTask;
    }

    [HttpGet("triggerPriceObvTrend")]
    [Authorize("admin")]
    public Task TriggerPriceObvTrend([FromServices] MonitoringServices.PriceObvTrendMonitoringService priceObvTrendAlerts)
    {
        priceObvTrendAlerts.Run();
        return Task.CompletedTask;
    }
        
    [HttpGet("triggerEmail")]
    [Authorize("admin")]
    public Task TriggerEmail(
        [FromServices] MonitoringServices.AlertEmailService emailAlerts)
    {
        emailAlerts.Execute();
        return Task.CompletedTask;
    }
        
    [HttpGet("triggerAccountMonitoring")]
    [Authorize("admin")]
    public Task TriggerAccountMonitoring(
        [FromServices] core.fs.Brokerage.MonitoringServices.AccountMonitoringService accountMonitoring)
        => accountMonitoring.RunAccountValueOrderAndTransactionSync();
    
    [HttpGet("triggerAccountTransactionProcessing")]
    [Authorize("admin")]
    public Task TriggerAccountTransactionProcessing(
        [FromServices] core.fs.Brokerage.MonitoringServices.AccountMonitoringService accountMonitoring)
        => accountMonitoring.RunTransactionProcessing();
    
    [HttpGet("triggerPortfolioProcessing")]
    [Authorize("admin")]
    public Task TriggerPortfolioProcessing(
        [FromServices] core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService service)
        => service.RecentlyClosedPositionUpdates();
    
    [HttpGet("triggerOptionPricing")]
    [Authorize("admin")]
    public Task TriggerOptionPricing(
        [FromServices] core.fs.Options.MonitoringServices.PriceMonitoringService optionPricing)
        => optionPricing.Run();
    
    [HttpGet("triggerPriceAlerts")]
    [Authorize("admin")]
    public Task TriggerPriceAlerts(
        [FromServices] MonitoringServices.PriceAlertMonitoringService priceAlerts)
        => priceAlerts.Execute();
    
    [HttpGet("triggerNearTriggerPriceAlerts")]
    [Authorize("admin")]
    public Task TriggerNearTriggerPriceAlerts(
        [FromServices] MonitoringServices.PriceAlertNearTriggerMonitoringService nearTriggerAlerts)
        => nearTriggerAlerts.Execute();
    
    [HttpGet("triggerReminders")]
    [Authorize("admin")]
    public Task TriggerReminders(
        [FromServices] MonitoringServices.ReminderMonitoringService reminderService)
        => reminderService.Execute();
    
    [HttpGet("triggerSECFilings")]
    [Authorize("admin")]
    public Task TriggerSECFilings(
        [FromServices] SECFilingsMonitoring.SECFilingsMonitoringService secFilingsService)
        => secFilingsService.Execute();
    
    // Stock Price Alerts CRUD
    [HttpGet("price")]
    public Task<ActionResult> GetStockPriceAlerts() =>
        this.OkOrError(handler.Handle(new GetStockPriceAlerts(User.Identifier())));
    
    [HttpPost("price")]
    public Task<ActionResult> CreateStockPriceAlert([FromBody] CreateStockPriceAlertRequest request) =>
        this.OkOrError(handler.Handle(new CreateStockPriceAlert(
            userId: User.Identifier(),
            ticker: request.Ticker,
            priceLevel: request.PriceLevel,
            alertType: request.AlertType,
            note: request.Note
        )));
    
    [HttpPut("price/{alertId}")]
    public Task<ActionResult> UpdateStockPriceAlert([FromRoute] System.Guid alertId, [FromBody] UpdateStockPriceAlertRequest request) =>
        this.OkOrError(handler.Handle(new UpdateStockPriceAlert(
            userId: User.Identifier(),
            alertId: alertId,
            priceLevel: request.PriceLevel,
            alertType: request.AlertType,
            note: request.Note,
            state: request.State
        )));
    
    [HttpPost("price/{alertId}/reset")]
    public Task<ActionResult> ResetStockPriceAlert([FromRoute] System.Guid alertId) =>
        this.OkOrError(handler.Handle(new ResetStockPriceAlert(
            userId: User.Identifier(),
            alertId: alertId
        )));
    
    [HttpDelete("price/{alertId}")]
    public Task<ActionResult> DeleteStockPriceAlert([FromRoute] System.Guid alertId) =>
        this.OkOrError(handler.Handle(new DeleteStockPriceAlert(alertId)));
    
    // Reminders CRUD
    [HttpGet("reminders")]
    public Task<ActionResult> GetReminders() =>
        this.OkOrError(handler.Handle(new GetReminders(User.Identifier())));
    
    [HttpPost("reminders")]
    public Task<ActionResult> CreateReminder([FromBody] CreateReminderRequest request) =>
        this.OkOrError(handler.Handle(new CreateReminder(
            userId: User.Identifier(),
            date: request.Date,
            message: request.Message,
            ticker: request.Ticker
        )));
    
    [HttpPut("reminders/{reminderId}")]
    public Task<ActionResult> UpdateReminder([FromRoute] System.Guid reminderId, [FromBody] UpdateReminderRequest request) =>
        this.OkOrError(handler.Handle(new UpdateReminder(
            userId: User.Identifier(),
            reminderId: reminderId,
            date: request.Date,
            message: request.Message,
            ticker: request.Ticker,
            state: request.State
        )));
    
    [HttpDelete("reminders/{reminderId}")]
    public Task<ActionResult> DeleteReminder([FromRoute] System.Guid reminderId) =>
        this.OkOrError(handler.Handle(new DeleteReminder(reminderId)));
}

// Request DTOs for API
public record CreateStockPriceAlertRequest(string Ticker, decimal PriceLevel, string AlertType, string Note);
public record UpdateStockPriceAlertRequest(decimal PriceLevel, string AlertType, string Note, string State);
public record CreateReminderRequest(string Date, string Message, string? Ticker);
public record UpdateReminderRequest(string Date, string Message, string? Ticker, string State);
