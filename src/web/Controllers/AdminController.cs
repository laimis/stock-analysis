using System.Collections.Generic;
using System.Threading.Tasks;
using core.Account;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private IMediator _mediator;

        public AdminController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpGet("users")]
        public async Task<object> UsersAsync()
        {
            var list = await this._mediator.Send<IEnumerable<LoginLogEntry>>(
                new GetLogins.Query());

            return list;
        }
    }
}