using System;
using System.IO;
using System.Threading.Tasks;
using core.Stocks;
using core.Stocks.Handlers;
using core.Stocks.View;
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
        public Task<object> Dashboard() => _mediator.Send(new Dashboard.Query(User.Identifier()));

        [HttpGet("{ticker}")]
        public Task<object> DetailsAsync(string ticker) => _mediator.Send(new Details.Query(ticker));

        [HttpGet("{ticker}/analysis")]
        public Task<object> Analysis(string ticker) => _mediator.Send(new Analysis.Query(ticker, User.Identifier()));

        [HttpGet("{ticker}/dailyanalysis")]
        public Task<object> DailyAnalysis(string ticker) => _mediator.Send(new Analysis.DailyQuery(ticker, User.Identifier()));

        [HttpGet("{ticker}/prices")]
        public Task<PricesView> Prices(string ticker, [FromQuery] int numberOfDays) => _mediator.Send(new Prices.Query(numberOfDays, ticker, User.Identifier()));

        [HttpGet("{ticker}/price")]
        public Task<decimal> Price(string ticker) => _mediator.Send(new Price.Query(ticker));
        
        [HttpGet("{ticker}/ownership")]
        public async Task<object> Ownership(string ticker)
        {
            var query = new Ownership.Query(ticker);
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

        [HttpPost("{ticker}/stop")]
        public async Task<ActionResult> Stop(SetStop.Command command)
        {
            command.WithUserId(User.Identifier());

            var r = await _mediator.Send(command);

            return this.OkOrError(r);
        }

        [HttpDelete("{ticker}/stop")]
        public async Task<ActionResult> DeleteStop(string ticker)
        {
            var command = new DeleteStop.Command(ticker);
            command.WithUserId(User.Identifier());

            var r = await _mediator.Send(command);

            return this.OkOrError(r);
        }

        [HttpPost("{ticker}/risk")]
        public async Task<ActionResult> Risk(SetRisk.Command model)
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
            return this.GenerateExport(_mediator, new ExportTransactions.Query(User.Identifier()));
        }

        [HttpGet("export/closed")]
        public Task<ActionResult> ExportClosed()
        {
            return this.GenerateExport(_mediator, new ExportTrades.Query(User.Identifier(), core.Stocks.ExportTrades.ExportType.Closed));
        }

        [HttpGet("export/open")]
        public Task<ActionResult> ExportTrades()
        {
            return this.GenerateExport(_mediator, new ExportTrades.Query(User.Identifier(), core.Stocks.ExportTrades.ExportType.Open));
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
        public Task<TradingEntriesView> TradingEntries() =>
            _mediator.Send(
                new TradingEntries.Query(User.Identifier())
            );
    }
}
