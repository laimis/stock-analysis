using System.Threading.Tasks;
using core.fs.Alerts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class AlertsController : ControllerBase
    {
        [AllowAnonymous]
        [Consumes("text/plain")]
        [HttpPost("sms")]
        public Task SMS([FromBody] string body, [FromServices] SMS.Handler service) =>
            service.Handle(new SMS.SendSMS(body));

        [AllowAnonymous]
        [HttpGet("sms/status")]
        public Task<ActionResult> SmsStatus([FromServices] SMS.Handler service) =>
            this.OkOrError(service.Handle(new SMS.Status()));

        [Authorize("admin")]
        [HttpPost("sms/on")]
        public Task SmsOn([FromServices] SMS.Handler service) => service.Handle(new SMS.TurnSMSOn());

        [Authorize("admin")]
        [HttpPost("sms/off")]
        public Task SmsOff([FromServices] SMS.Handler service) => service.Handle(new SMS.TurnSMSOff());

        [Authorize("admin")]
        [HttpPost("run")]
        public ActionResult Run([FromServices] AlertContainer.Handler service)
        {
            service.Handle(new AlertContainer.Run());
            return Ok();
        }

        // TODO: consider returning a specific type vs object
        [HttpGet]
        public object Index([FromServices]AlertContainer.Handler service) =>
            service.Handle(new AlertContainer.Query(User.Identifier()));
            
        // TODO: consider returning a specific type vs object
        [HttpGet("monitors")]
        public object Monitors([FromServices]AlertContainer.Handler service) =>
            service.Handle(new AlertContainer.QueryAvailableMonitors());
    }
}
