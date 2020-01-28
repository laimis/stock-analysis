using System.IO;
using System.Threading.Tasks;
using core;
using core.Stocks;
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
        public async Task<ActionResult> Sell(Sell.Command model)
        {
            model.WithUserId(this.User.Identifier());

            await _mediator.Send(model);

            return Ok();
        }

        [HttpPost("purchase")]
        public async Task<ActionResult> Purchase(Buy.Command model)
        {
            model.WithUserId(this.User.Identifier());

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
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(this.User.Identifier()));
        }

        [HttpPost("import")]
        public async Task Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            cmd.WithUserId(this.User.Identifier());

            await _mediator.Send(cmd);
        }
    }
}
