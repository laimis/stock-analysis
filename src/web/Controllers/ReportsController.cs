using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Portfolio;
using core.Portfolio.Output;
using core.Reports;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            this._mediator = mediator;
        }

        [HttpGet("chain")]
        public object Chain()
        {
            // var query = new Chain.Query(this.User.Identifier());

            // return _mediator.Send(query);

            var links = new List<object>();

            var rand = new Random();

            while(links.Count < 241)
            {
                links.Add(new {success = rand.NextDouble() >= 0.5});
            }

            return new {links};
        }
    }
}