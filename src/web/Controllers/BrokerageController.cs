using System.Threading.Tasks;
using core.Brokerage;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
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
    }
}