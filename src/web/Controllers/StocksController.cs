using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using core;
using core.Adapters.Stocks;
using core.Stocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class StocksController : ControllerBase
    {
        private IMediator _mediator;

        public StocksController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("{ticker}")]
        public async Task<object> DetailsAsync(string ticker)
        {
            return await _mediator.Send(new Details.Query(ticker));
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(SellTransaction.Command model)
        {
            model.WithUser(this.User.Identifier());

            await _mediator.Send(model);

            return Ok();
        }

        [HttpPost("purchase")]
        public async Task<ActionResult> Purchase(BuyTransaction.Command model)
        {
            model.WithUser(this.User.Identifier());

            await _mediator.Send(model);

            return Ok();
        }

        [HttpGet("lists")]
        public async Task<object> MostActive()
        {
            var active = _mediator.Send(new StockLists.QueryMostActive());
            var gainers = _mediator.Send(new StockLists.QueryGainers());
            var losers = _mediator.Send(new StockLists.QueryLosers());

            await Task.WhenAll(active, gainers, losers);

            return Mapper.MapLists(
                active.Result,
                gainers.Result,
                losers.Result);
        }

        [HttpGet("export")]
        public async Task<ActionResult> Export()
        {
            var response = await _mediator.Send(new Export.Query(this.User.Identifier()));

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
