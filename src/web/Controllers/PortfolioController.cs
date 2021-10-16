using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using core.Portfolio;
using core.Portfolio.Output;
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

        [HttpGet("review")]
        public async Task<ReviewView> Review(string period)
        {
            var cmd = new Review.Generate(period);

            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }

        [HttpGet("grid")]
        public async Task<IEnumerable<GridEntry>> Grid()
        {
            var cmd = new Grid.Generate();
            cmd.WithUserId(User.Identifier());

            return await _mediator.Send(cmd);
        }
    }
}