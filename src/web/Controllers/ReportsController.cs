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

        [HttpGet("analysis/portfolio")]
        public Task<AnalysisReportView> AnalysisReportPortfolio([FromQuery]PriceFrequency frequency)
            => _mediator.Send(new AnalysisReport.ForPortfolioQuery(frequency, User.Identifier()));

        

        [HttpGet("outcomes/portfolio")]
        public Task<List<TickerOutcomes>> Analysis([FromQuery]PriceFrequency frequency) =>
            _mediator.Send(new OutcomesReport.ForPortfolioQuery(frequency, User.Identifier()));
    }
}