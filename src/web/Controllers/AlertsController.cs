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
        public async Task<object> List()
        {
            var query = new List.Query(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("diagnostics")]
        public object Diagnostics()
        {
            return _container.Monitors.Select(
                m => new {
                    m.Alert.Ticker,
                    m.Alert.PricePoints
                }
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

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(User.Identifier()));
        }

        [HttpPost("delete")]
        public async Task<object> Delete(Delete.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpGet("{ticker}")]
        public async Task<object> Get(string ticker)
        {
            var query = new Get.Query(User.Identifier(), ticker);

            return await _mediator.Send(query);
        }

        [HttpPost]
        public async Task<object> AddPricePoint(Create.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }
    }
}
