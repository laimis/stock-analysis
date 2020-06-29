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

        public AlertsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public async Task<object> List()
        {
            var query = new List.Query(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("diagnostics")]
        public object Diagnostics()
        {
            return StockMonitorService._monitors.Select(
                kp => kp.Value
            );
        }

        [HttpGet("migrate")]
        public async Task<object> Migrate()
        {
            var query = new Migrate.Command(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("clear")]
        public async Task<object> Clear()
        {
            var query = new Clear.Command(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("{ticker}")]
        public async Task<object> Get(string ticker)
        {
            var query = new Get.Query(User.Identifier(), ticker);

            return await _mediator.Send(query);
        }
    }
}
