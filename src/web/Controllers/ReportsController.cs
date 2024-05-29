using System;
using System.Threading.Tasks;
using core.fs.Adapters.Stocks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using core.fs.Reports;
using core.fs.Stocks;
using core.Shared;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController(Handler service) : ControllerBase
    {
        [HttpGet("chain")]
        public Task<ChainView> Chain() =>
            service.Handle(
                new ChainQuery(
                    User.Identifier()
                )
            );

        [HttpGet("sells")]
        public Task<ActionResult> Sells() =>
            this.OkOrError(
                service.Handle(
                    new SellsQuery(
                        User.Identifier()
                    )
                )
            );

        [HttpPost("outcomes")]
        public Task<ActionResult> TickersOutcomes(
            [FromBody]OutcomesReportQuery query) =>
            this.OkOrError(service.HandleOutcomesReport(User.Identifier(), query));

        [HttpGet("percentChangeDistribution/tickers/{ticker}")]
        public Task<ActionResult> TickerPercentChangeDistribution(string ticker, [FromQuery] string frequency)
            => this.OkOrError(
                service.Handle(
                    new PercentChangeStatisticsQuery(
                        PriceFrequency.FromString(frequency ?? PriceFrequency.Daily.ToString()), new Ticker(ticker),
                        User.Identifier())
                )
            );

        [HttpGet("gaps/tickers/{ticker}")]
        public Task<ActionResult> TickerGaps(string ticker, [FromQuery] string frequency)
            => this.OkOrError(
                service.Handle(
                    new GapReportQuery(User.Identifier(), new Ticker(ticker), PriceFrequency.FromString(frequency))
                )
            );

        [HttpGet("positions")]
        public Task<ActionResult> Portfolio() =>
            this.OkOrError(
                service.Handle(
                    new OutcomesReportForPositionsQuery(User.Identifier())
                )
            );

        [HttpGet("DailyPositionReport/{positionId}")]
        public Task<ActionResult> DailyPositionReport(string positionId) =>
            this.OkOrError(
                service.Handle(
                    new DailyPositionReportQuery(
                        User.Identifier(),
                        StockPositionId.NewStockPositionId(Guid.Parse(positionId)
                        )
                    )
                )
            );
        
        [HttpGet("DailyTickerReport/{ticker}")]
        public Task<ActionResult> DailyTickerReport(string ticker) =>
            this.OkOrError(
                service.Handle(
                    new DailyTickerReportQuery(
                        User.Identifier(),
                        new Ticker(ticker)
                    )
                )
            );
        
        [HttpGet("trends/{ticker}")]
        public Task<ActionResult> Trends(string ticker, [FromQuery] string start, [FromQuery] string end, [FromQuery] string trendType) =>
            this.OkOrError(
                service.Handle(
                    new TrendsQuery(
                        User.Identifier(),
                        new Ticker(ticker),
                        core.fs.Services.Trends.TrendType.FromString(trendType),
                        start, end
                    )
                )
            );
    }
}
