using System;
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
            this._mediator = mediator;
        }

        [HttpGet]
        public Task<object> Index()
        {
            var query = new Get.Query(this.User.Identifier());

            return _mediator.Send(query);
        }

        [HttpGet("transactions")]
        public Task<TransactionList> TransactionsAsync(string ticker, string groupBy, string show)
        {
            var query = new Transactions.Query(this.User.Identifier(), ticker, groupBy, show);

            return _mediator.Send(query);
        }

        [HttpGet("review")]
        public async Task<ReviewList> Review()
        {
            var cmd = new Review.Generate(DateTime.UtcNow);

            cmd.WithUserId(this.User.Identifier());

            return await _mediator.Send(cmd);
        }
    }
}