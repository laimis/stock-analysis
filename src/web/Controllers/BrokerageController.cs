using System.Threading.Tasks;
using core.Brokerage;
using core.Shared.Adapters.Brokerage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class BrokerageController : ControllerBase
    {
        private ILogger<AccountController> _logger;
        private IMediator _mediator;

        public BrokerageController(ILogger<AccountController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }
        
        [HttpPost("buy")]
        public async Task<ActionResult> Buy(Buy.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(User.Identifier());

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpDelete("orders/{orderId}")]
        public async Task<ActionResult> Delete(string orderId)
        {
            var r = await _mediator.Send(new CancelOrder.Command(orderId, User.Identifier()));

            return this.OkOrError(r);
        }

        [HttpGet("orders")]
        public Task<Order[]> GetOrders() => _mediator.Send(new Orders.Query(User.Identifier()));
    }
}