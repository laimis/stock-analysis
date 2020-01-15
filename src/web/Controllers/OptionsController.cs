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
            var details = await _mediator.Send(new GetOptionDetails.Query(ticker));
            if (details == null)
            {
                return NotFound();
            }
            
            return details;
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(SellOption.Command cmd)
        {
            cmd.WithUser(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("soldoptions/{ticker}/{type}/{strikePrice}/{expiration}")]
        public async Task<object> SoldOption(string ticker, string type, double strikePrice, DateTimeOffset expiration)
        {
            // TODO: can this come from the route?
            var query = new GetSoldOption.Query {
                Expiration = expiration,
                Ticker = ticker,
                Type = type,
                StrikePrice = strikePrice,
                UserId = this.User.Identifier()
            };
            
            var sold =  await _mediator.Send(query);

            if (sold == null)
            {
                return NotFound();
            }

            return sold;
        }

        [HttpPost("close")]
        public async Task<ActionResult> CloseSoldOption(CloseOption.Command cmd)
        {
            cmd.WithUser(this.User.Identifier());

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("export")]
        public async Task<ActionResult> Export()
        {
            var response = await _mediator.Send(
                new Export.Query(this.User.Identifier())
            );

            this.HttpContext.Response.Headers.Add(
                "content-disposition", 
                $"attachment; filename={response.Filename}");

            return new ContentResult
            {
                Content = response.Content,
                ContentType = response.ContentType
            };
        }
    }
}