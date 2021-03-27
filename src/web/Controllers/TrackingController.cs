using System;
using System.IO;
using System.Threading.Tasks;
using core.Stocks;
using core.Tracking.Handlers;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class TrackingController : ControllerBase
    {
        private IMediator _mediator;

        public TrackingController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{ticker}/register")]
        public async Task<object> Register(string ticker)
        {
            var cmd = new Register.Command(ticker);
            cmd.WithUserId(User.Identifier());
            return await _mediator.Send(cmd);
        }
    }
}
