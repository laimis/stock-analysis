using System.Collections.Generic;
using System.Threading.Tasks;
using core.Alerts;
using core.Stocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

        [HttpGet("triggered")]
        public Task<List<PositionInstance>> Triggered() =>
            _mediator.Send(new Triggered.Query());
    }
}
