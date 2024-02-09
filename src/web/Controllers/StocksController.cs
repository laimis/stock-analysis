using System;
using System.Threading.Tasks;
using core.fs.Adapters.Stocks;
using core.fs.Stocks;
using core.Shared;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class StocksController(StocksHandler service) : ControllerBase
    {
        [HttpGet("{ticker}")]
        public Task<ActionResult> Details([FromRoute] string ticker)
            => this.OkOrError(
                service.Handle(
                    new DetailsQuery(new Ticker(ticker), User.Identifier())
                )
            );

        [HttpGet("{ticker}/prices")]
        public Task<ActionResult> Prices([FromRoute] string ticker, [FromQuery] int numberOfDays,
            [FromQuery] string frequency) =>
            this.OkOrError(
                service.Handle(
                    PricesQuery.NumberOfDays(
                        frequency: PriceFrequency.FromString(frequency), numberOfDays: numberOfDays,
                        ticker: new Ticker(ticker), userId: User.Identifier()
                    )
                )
            );

        [HttpGet("{ticker}/secfilings")]
        public Task<ActionResult> SecFilings([FromRoute] string ticker) =>
            this.OkOrError(
                service.Handle(
                    new CompanyFilingsQuery(
                        new Ticker(ticker), User.Identifier()
                    )
                )
            );

        [HttpGet("{ticker}/prices/{start}/{end}")]
        public Task<ActionResult> Prices(
            [FromRoute] string ticker,
            [FromRoute] DateTimeOffset start,
            [FromRoute] DateTimeOffset end,
            [FromQuery] string frequency) =>
            this.OkOrError(
                service.Handle(
                    new PricesQuery(
                        frequency: PriceFrequency.FromString(frequency), userId: User.Identifier(),
                        ticker: new Ticker(ticker), start: start, end: end
                    )
                )
            );

        [HttpGet("{ticker}/price")]
        public Task<ActionResult> Price([FromRoute] string ticker) =>
            this.OkOrError(
                service.Handle(
                    new PriceQuery(userId: User.Identifier(), ticker: new Ticker(ticker))
                )
            );

        [HttpGet("{ticker}/quote")]
        public Task<ActionResult> Quote([FromRoute] string ticker) =>
            this.OkOrError(
                service.Handle(
                    new QuoteQuery(
                        new Ticker(ticker), User.Identifier()
                    )
                )
            );

        [HttpGet("search/{term}")]
        public Task<ActionResult> Search([FromRoute] string term) =>
            this.OkOrError(
                service.Handle(
                    new SearchQuery(term, User.Identifier())
                )
            );
    }
}
