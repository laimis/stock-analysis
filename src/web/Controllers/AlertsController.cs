using System.Threading.Tasks;
using core.Alerts;
using core.Alerts.Handlers;
using MediatR;
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
        private IMediator _mediator;

        public AlertsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [AllowAnonymous]
        [Consumes("text/plain")]
        [HttpPost("sms")]
        public Task SMS([FromBody] string body) =>
            _mediator.Send(new SendSMS.Command(body));

        [AllowAnonymous]
        [HttpGet("sms/status")]
        public Task<bool> SmsStatus() => _mediator.Send(new SmsStatus.Query());

        [Authorize("admin")]
        [HttpPost("sms/on")]
        public Task SmsOn() => _mediator.Send(new SmsOn.Command());

        [Authorize("admin")]
        [HttpPost("sms/off")]
        public Task SmsOff() => _mediator.Send(new SmsOff.Command());

        [Authorize("admin")]
        [HttpPost("run")]
        public Task Run() => _mediator.Send(new Run.Command(User.Identifier()));

        [HttpGet]
        public Task<object> Index() =>
            _mediator.Send(new Get.Query(User.Identifier()));

        [HttpGet("monitors")]
        public Task<Monitors.MonitorDescriptor[]> Monitors() =>
            _mediator.Send(new Monitors.Query(User.Identifier()));
    }
}
