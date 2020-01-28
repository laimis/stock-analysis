using System;
using System.Threading.Tasks;
using core.Options;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class OptionsController : ControllerBase
    {
        private IMediator _mediator;

        public OptionsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{ticker}")]
        public async Task<ActionResult<OptionDetailsViewModel>> DetailsAsync(string ticker)
        {
            var details = await _mediator.Send(new Detail.Query(ticker));
            if (details == null)
            {
                return NotFound();
            }
            
            return details;
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(Sell.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("soldoptions/{id}")]
        public async Task<object> SoldOption(Guid id)
        {
            var query = new Get.Query { Id = id };

            query.WithUserId(this.User.Identifier());
            
            var sold =  await _mediator.Send(query);

            if (sold == null)
            {
                return NotFound();
            }

            return sold;
        }

        [HttpPost("close")]
        public async Task<ActionResult> CloseSoldOption(Close.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }
    }
}