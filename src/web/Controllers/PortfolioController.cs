using System.Collections.Generic;
using System.Threading.Tasks;
using core.Portfolio;
using core.Shared;
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
        public async Task<object> Index()
        {
            var query = new Get.Query(this.User.Identifier());

            return await _mediator.Send(query);
        }

        [HttpGet("transactions")]
        public async Task<IEnumerable<Transaction>> TransactionsAsync(string ticker)
        {
            var query = new Transactions.Query(this.User.Identifier(), ticker);

            return await _mediator.Send(query);
        }
    }
}