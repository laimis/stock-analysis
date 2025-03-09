using System.Collections.Generic;
using System.Threading.Tasks;
using core.fs.Adapters.Logging;
using core.fs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

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
}
