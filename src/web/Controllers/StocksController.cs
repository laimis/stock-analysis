using System;
using System.IO;
using System.Threading.Tasks;
using core.Stocks;
using core.Stocks.Views;
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

        [HttpGet()]
        public async Task<object> Dashboard()
        {
            return await _mediator.Send(new Dashboard.Query(User.Identifier()));
        }

        [HttpGet("{ticker}")]
        public async Task<object> DetailsAsync(string ticker)
        {
            return await _mediator.Send(new Details.Query(ticker));
        }

        [HttpGet("{ticker}/ownership")]
        public async Task<object> Ownership(string ticker)
        {
            var query = new Ownership.Query(ticker);
            query.WithUserId(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("{ticker}/ownership/raw")]
        public async Task<object> Raw(string ticker)
        {
            var query = new Ownership.Query(ticker, true);
            query.WithUserId(User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpDelete("{id}")]
        public async Task<object> Delete(Guid id)
        {
            var cmd = new Delete.Command(id, User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpDelete("{id}/transactions/{eventId}")]
        public async Task<object> DeleteTransaction(Guid id, Guid eventId)
        {
            var cmd = new DeleteTransaction.Command(id, eventId, User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpGet("search/{term}")]
        public async Task<object> Search(string term)
        {
            return await _mediator.Send(new Search.Query(term));
        }

        [HttpPost("settings")]
        public async Task<ActionResult> Settings(Settings.Command model)
        {
            model.WithUserId(User.Identifier());

            var r = await _mediator.Send(model);

            return this.OkOrError(r);
        }

        [HttpPost("sell")]
        public async Task<ActionResult> Sell(Sell.Command model)
        {
            model.WithUserId(User.Identifier());

            var r = await _mediator.Send(model);

            return this.OkOrError(r);
        }

        [HttpPost("purchase")]
        public async Task<ActionResult> Purchase(Buy.Command model)
        {
            model.WithUserId(User.Identifier());

            var r = await _mediator.Send(model);

            return this.OkOrError(r);
        }

        [HttpGet("export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Export.Query(User.Identifier()));
        }

        [HttpGet("export/closed")]
        public Task<ActionResult> ExportClosed()
        {
            return this.GenerateExport(_mediator, new ExportClosed.Query(User.Identifier()));
        }

        [HttpGet("export/owned")]
        public Task<ActionResult> ExportOwned()
        {
            return this.GenerateExport(_mediator, new ExportOpen.Query(User.Identifier()));
        }

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new Import.Command(content);

            cmd.WithUserId(User.Identifier());

            return this.OkOrError(await _mediator.Send(cmd));
        }

        [HttpGet("tradingentries")]
        public Task<TradingEntryView[]> TradingEntries()
        {
            var query = new TradingEntries.Query(User.Identifier());

            return _mediator.Send(query);
        }
    }
}
