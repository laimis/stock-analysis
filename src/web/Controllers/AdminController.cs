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
    }
}