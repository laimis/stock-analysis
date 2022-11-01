using System.Collections.Generic;
using System.Threading.Tasks;
using core.Reports;
using core.Reports.Views;
using core.Shared.Adapters.Stocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private IMediator _mediator;

        public ReportsController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("chain")]
        public Task<object> Chain()
        {
            var query = new Chain.Query(User.Identifier());

            return _mediator.Send(query);
        }

        [HttpGet("sells")]
        public Task<SellsView> Sells()
        {
            var query = new Sells.Query(User.Identifier());

            return _mediator.Send(query);
        }

        [HttpGet("outcomes/portfolio")]
        public Task<OutcomesReportView> PortfolioOutcomes(
            [FromQuery] OutcomesReport.Duration duration,
            [FromQuery] PriceFrequency frequency,
            [FromQuery] bool includeGapAnalysis) =>
            _mediator.Send(new OutcomesReport.ForPortfolioQuery(duration, frequency, includeGapAnalysis, User.Identifier()));

        [HttpGet("outcomes/ticker/{ticker}")]
        public Task<OutcomesReportView> TickerOutcomes(
            string ticker,
            [FromQuery] PriceFrequency priceFrequency,
            [FromQuery] OutcomesReport.Duration duration,
            [FromQuery] bool includeGapAnalysis) =>
            _mediator.Send(new OutcomesReport.ForTickerQuery(duration, priceFrequency, includeGapAnalysis, ticker, User.Identifier()));

        [HttpPost("outcomes/tickers")]
        public Task<OutcomesReportView> TickersOutcomes(
            [FromBody]string[] tickers,
            [FromQuery] OutcomesReport.Duration duration,
            [FromQuery] PriceFrequency frequency,
            [FromQuery] bool includeGapAnalysis) =>
            _mediator.Send(new OutcomesReport.ForTickersQuery(duration, frequency, includeGapAnalysis, tickers, User.Identifier()));

        [HttpGet("analysis/ticker/{ticker}")]
        public Task<AnalysisReportView> TickerAnalysis(string ticker, [FromQuery]PriceFrequency frequency)
            => _mediator.Send(new SingleBarAnalysisReport.ForTickerQuery(frequency, ticker, User.Identifier()));

        [HttpGet("analysis/portfolio")]
        public Task<AnalysisReportView> PortfolioAnalysis([FromQuery]PriceFrequency frequency)
            => _mediator.Send(new SingleBarAnalysisReport.ForPortfolioQuery(frequency, User.Identifier()));

        [HttpPost("analysis/tickers")]
        public Task<AnalysisReportView> TickersAnalysis([FromBody]string[] tickers, [FromQuery] PriceFrequency frequency) =>
            _mediator.Send(new SingleBarAnalysisReport.ForTickersQuery(frequency, tickers, User.Identifier()));

        [HttpGet("percentChangeDistribution/tickers/{ticker}")]
        public Task<PercentChangeStatisticsView> TickerPercentChangeDistribution(string ticker, [FromQuery] PriceFrequency frequency)
            => _mediator.Send(new PercentChangeStatistics.ForTickerQuery(frequency, ticker, User.Identifier()));

        [HttpGet("gaps/tickers/{ticker}")]
        public Task<GapsView> TickerGaps(string ticker, [FromQuery] PriceFrequency frequency)
            => _mediator.Send(new GapReport.ForTickerQuery(frequency, ticker, User.Identifier()));
    }
}