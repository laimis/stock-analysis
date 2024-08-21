using System.Collections.Generic;
using System.Threading.Tasks;
using core.fs.Adapters.Logging;
using core.fs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
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
        
        [HttpGet("triggerDaily")]
        [Authorize("admin")]
        public Task TriggerDaily([FromServices] MonitoringServices.PatternMonitoringService dailyAlerts)
        {
            dailyAlerts.Execute();
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
        
        [HttpGet("triggerDayAnalysis")]
        [Authorize("admin")]
        public Task TriggerDayAnalysis(
            [FromServices] core.fs.Portfolio.MonitoringServices.PortfolioAnalysisService portfolioAnalysis) =>
                portfolioAnalysis.ReportOnMaxProfitBasedOnDaysHeld();
    }
}
