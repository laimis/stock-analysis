using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using core.fs.Reports;
using core.fs.Shared.Adapters.Stocks;
using core.Shared;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly Handler _service;

        public ReportsController(Handler service)
        {
            _service = service;
        }
        
        [HttpGet("chain")]
        public Task<ActionResult> Chain() =>
            this.OkOrError(
                _service.Handle(
                    new ChainQuery(
                        User.Identifier()
                    )
                )
            );

        [HttpGet("sells")]
        public Task<ActionResult> Sells() =>
            this.OkOrError(
                _service.Handle(
                    new SellsQuery(
                        User.Identifier()
                    )
                )
            );

        [HttpPost("outcomes")]
        public Task<ActionResult> TickersOutcomes(
            [FromBody]OutcomesReportQuery query)
        {
            var withUserId = OutcomesReportQuery.WithUserId(User.Identifier(), query);
            
            return this.OkOrError(_service.Handle(withUserId));
        }

        [HttpGet("percentChangeDistribution/tickers/{ticker}")]
        public Task<ActionResult> TickerPercentChangeDistribution(string ticker, [FromQuery] string frequency)
            => this.OkOrError(_service.Handle(new PercentChangeStatisticsQuery(PriceFrequency.FromString(frequency), new Ticker(ticker), User.Identifier())));

        [HttpGet("gaps/tickers/{ticker}")]
        public Task<ActionResult> TickerGaps(string ticker, [FromQuery] string frequency)
            => this.OkOrError(_service.Handle(new GapReportQuery(User.Identifier(), new Ticker(ticker), PriceFrequency.FromString(frequency))));

        [HttpGet("positions")]
        public Task<ActionResult> Portfolio() =>
            this.OkOrError(_service.Handle(new OutcomesReportForPositionsQuery(User.Identifier())));

        [HttpGet("DailyPositionReport/{ticker}/{positionId}")]
        public Task<ActionResult> DailyPositionReport(string ticker, int positionId) =>
            this.OkOrError(_service.Handle(new DailyPositionReportQuery(User.Identifier(), new Ticker(ticker), positionId)));
    }
}