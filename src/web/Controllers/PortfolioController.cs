using System.Threading.Tasks;
using core.Portfolio;
using core.Portfolio.Output;
using core.Stocks;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/[controller]")]
    public class PortfolioController : ControllerBase
    {
        private IMediator _mediator;

        public PortfolioController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public Task<PortfolioResponse> Index()
        {
            var query = new Get.Query(User.Identifier());

            return _mediator.Send(query);
        }

        [HttpGet("transactions")]
        public Task<TransactionList> TransactionsAsync(string ticker, string groupBy, string show, string txType)
        {
            var query = new Transactions.Query(User.Identifier(), ticker, groupBy, show, txType);

            return _mediator.Send(query);
        }

        [HttpGet("transactionsummary")]
        public async Task<TransactionSummaryView> Review(string period)
        {
            var cmd = new TransactionSummary.Generate(period);

            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpGet("{ticker}/positions/{positionId}/simulate/{stategyName}")]
        public Task<PositionInstance> Trade(
            int positionId,
            string strategyName,
            string ticker) =>
            
            _mediator.Send(
                new SimulateTrade.Command(positionId, strategyName, ticker, User.Identifier())
            );
    }
}