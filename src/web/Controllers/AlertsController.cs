using System.Linq;
using System.Threading.Tasks;
using core.Alerts;
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

        [HttpGet]
        public Task<object> List() => _mediator.Send(new List.Query(User.Identifier()));

        [HttpGet("diagnostics")]
        public object Diagnostics() => _container.Monitors.Select(
                m => new {
                    m.Alert.Ticker,
                    m.Alert.PricePoints
                }
            );

        [HttpGet("clear")]
        public Task<object> Clear() =>
            _mediator.Send(
                new Clear.Command(User.Identifier())
            );

        [HttpGet("export")]
        public Task<ActionResult> Export() => this.GenerateExport(_mediator, new Export.Query(User.Identifier()));

        [HttpPost("delete")]
        public async Task<object> Delete(Delete.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpGet("{ticker}")]
        public Task<object> Get(string ticker) => _mediator.Send(new Get.Query(User.Identifier(), ticker));

        [HttpPost]
        public async Task<object> AddPricePoint(Create.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }

        [AllowAnonymous]
        [Consumes("text/plain")]
        [HttpPost("sms")]
        public Task SMS([FromBody] string body) =>
            _mediator.Send(new SendSMS.Command(body));
    }
}
