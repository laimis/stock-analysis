using System;
using System.IO;
using System.Threading.Tasks;
using core.fs.Shared.Adapters.Stocks;
using core.fs.Stocks;
using core.Shared;
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
        private readonly Handler _service;

        public StocksController(Handler service) =>
            _service = service;

        [HttpGet]
        public Task<ActionResult> Dashboard() => this.OkOrError(_service.Handle(new DashboardQuery(User.Identifier())));

        [HttpGet("{ticker}")]
        public Task<ActionResult> Details([FromRoute]string ticker) => this.OkOrError(_service.Handle(new DetailsQuery(new Ticker(ticker), User.Identifier())));

        [HttpGet("{ticker}/prices")]
        public Task<ActionResult> Prices([FromRoute]string ticker, [FromQuery] int numberOfDays, [FromQuery] string frequency) =>
            this.OkOrError(_service.Handle(PricesQuery.NumberOfDays(frequency: PriceFrequency.FromString(frequency), numberOfDays: numberOfDays, ticker: new Ticker(ticker), userId: User.Identifier())));

        [HttpGet("{ticker}/secfilings")]
        public Task<ActionResult> SecFilings([FromRoute] string ticker) =>
            this.OkOrError(_service.Handle(new CompanyFilingsQuery(new Ticker(ticker), User.Identifier())));

        [HttpGet("{ticker}/prices/{start}/{end}")]
        public Task<ActionResult> Prices(
            [FromRoute] string ticker,
            [FromRoute] DateTimeOffset start,
            [FromRoute] DateTimeOffset end,
            [FromQuery] string frequency) =>
            this.OkOrError(_service.Handle(new PricesQuery(frequency: PriceFrequency.FromString(frequency), userId: User.Identifier(), ticker: new Ticker(ticker), start: start, end:end)));

        [HttpGet("{ticker}/price")]
        public Task<ActionResult> Price([FromRoute] string ticker) => 
            this.OkOrError(_service.Handle(new PriceQuery(userId: User.Identifier(), ticker: new Ticker(ticker))));

        [HttpGet("{ticker}/quote")]
        public Task<ActionResult> Quote([FromRoute] string ticker) =>
            this.OkOrError(_service.Handle(new QuoteQuery(new Ticker(ticker), User.Identifier())));
        
        [HttpGet("{ticker}/ownership")]
        public Task<ActionResult> Ownership(string ticker) => 
            this.OkOrError(_service.Handle(new OwnershipQuery(new Ticker(ticker), User.Identifier())));

        [HttpGet("search/{term}")]
        public Task<ActionResult> Search([FromRoute] string term) => 
            this.OkOrError(_service.Handle(new SearchQuery(term, User.Identifier())));

        [HttpPost("{ticker}/stop")]
        public Task<ActionResult> Stop([FromBody] SetStop command) =>
            this.OkOrError(_service.HandleSetStop(User.Identifier(), command));
        
        [HttpPost("sell")]
        public Task<ActionResult> Sell([FromBody]StockTransaction model) =>
            this.OkOrError(_service.Handle(BuyOrSell.NewSell(model, User.Identifier())));

        [HttpPost("buy")]
        public async Task<ActionResult> Buy([FromBody]StockTransaction model) =>
            this.OkOrError(await _service.Handle(BuyOrSell.NewBuy(model, User.Identifier())));
        
        [HttpGet("export")]
        public Task<ActionResult> Export() =>
            this.GenerateExport(_service.Handle(new ExportTransactions(User.Identifier())));
        
        [HttpGet("export/closed")]
        public Task<ActionResult> ExportClosed() =>
            this.GenerateExport(_service.Handle(new ExportTrades(User.Identifier(), ExportType.Closed)));

        [HttpGet("export/open")]
        public Task<ActionResult> ExportTrades() =>
            this.GenerateExport(_service.Handle(new ExportTrades(User.Identifier(), ExportType.Open)));

        [HttpPost("import")]
        public async Task<ActionResult> Import(IFormFile file)
        {
            using var streamReader = new StreamReader(file.OpenReadStream());

            var content = await streamReader.ReadToEndAsync();

            var cmd = new ImportStocks(userId: User.Identifier(), content: content);
            
            return await this.OkOrError(_service.Handle(cmd));
        }
    }
}
