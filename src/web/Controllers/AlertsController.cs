using System.Collections.Generic;
using System.Threading.Tasks;
using core.Alerts;
using core.Stocks;
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
        private StockMonitorContainer _container;

        public AlertsController(
            IMediator mediator,
            StockMonitorContainer container)
        {
            _mediator = mediator;
            _container = container;
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

        [HttpGet]
        public Task<object> Index() =>
            _mediator.Send(new core.Alerts.Get.Query(User.Identifier()));
    }
}
