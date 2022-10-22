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
        public Task<List<TickerOutcomes>> PortfolioOutcomes([FromQuery] OutcomesReport.Duration duration, [FromQuery] PriceFrequency frequency) =>
            _mediator.Send(new OutcomesReport.ForPortfolioQuery(duration, frequency, User.Identifier()));

        [HttpGet("outcomes/ticker/{ticker}")]
        public Task<List<TickerOutcomes>> TickerOutcomes(string ticker, [FromQuery] PriceFrequency priceFrequency, [FromQuery] OutcomesReport.Duration duration) =>
            _mediator.Send(new OutcomesReport.ForTickerQuery(duration, priceFrequency, ticker, User.Identifier()));

        [HttpPost("outcomes/tickers")]
        public Task<List<TickerOutcomes>> TickersOutcomes([FromBody]string[] tickers, [FromQuery] OutcomesReport.Duration duration, [FromQuery] PriceFrequency frequency) =>
            _mediator.Send(new OutcomesReport.ForTickersQuery(duration, frequency, tickers, User.Identifier()));

        [HttpGet("analysis/ticker/{ticker}")]
        public Task<AnalysisReportView> TickerAnalysis(string ticker, [FromQuery]PriceFrequency frequency)
            => _mediator.Send(new SingleBarAnalysisReport.ForTickerQuery(frequency, ticker, User.Identifier()));

        [HttpGet("analysis/portfolio")]
        public Task<AnalysisReportView> PortfolioAnalysis([FromQuery]PriceFrequency frequency)
            => _mediator.Send(new SingleBarAnalysisReport.ForPortfolioQuery(frequency, User.Identifier()));
    }
}