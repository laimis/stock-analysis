using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private IMediator _mediator;
        private IAccountStorage _storage;
        private IPortfolioStorage _portfolio;
        private IEmailService _email;

        public AdminController(
            IMediator mediator,
            IAccountStorage storage,
            IPortfolioStorage portfolio,
            IEmailService email)
        {
            _mediator = mediator;
            _storage = storage;
            _portfolio = portfolio;
            _email = email;
        }

        [HttpGet("loginas/{userId}")]
        public async Task<ActionResult> LoginAs(Guid userId)
        {
            var u = await _storage.GetUser(userId);

            await AccountController.EstablishSignedInIdentity(HttpContext, u);

            return this.Redirect("~/");
        }

        [HttpPost("email")]
        public async Task<ActionResult> Email(EmailInput obj)
        {
            await this._email.Send(
                obj.To,
                obj.From,
                obj.Subject,
                obj.Body
            );

            return this.Ok();
        }

        [HttpGet("users")]
        public async Task<ActionResult> ActiveAccountsAsync()
        {
            var users = await _storage.GetUsers();

            var sb = new StringBuilder();

            sb.Append(@"<html><body><table><tr>
                <th>Email</th>
                <th>User Id</th>
                <th>Last Login</th>
                <th>Stocks</th>
                <th>Options</th>
                <th>Notes</th>
            </tr>");

            foreach(var (email,userId) in users)
            {
                sb.Append($"<tr>");
                sb.Append($"<td>{email}</td>");
                sb.Append($"<td>{userId}</td>");

                var guid = new System.Guid(userId);

                var user = await _storage.GetUser(guid);

                sb.Append($"<td>{user?.LastLogin?.ToString()}</td>");
                
                var options = await _portfolio.GetOwnedOptions(guid);
                var notes = await _portfolio.GetNotes(guid);
                var stocks = await _portfolio.GetStocks(guid);

                sb.Append($"<td>{stocks.Count()}</td>");
                sb.Append($"<td>{options.Count()}</td>");
                sb.Append($"<td>{notes.Count()}</td>");

                sb.Append("</tr>");
            }

            sb.Append("<table></body></html>");

            return new ContentResult {
                Content = sb.ToString(),
                ContentType = "text/html"
            };
        }
    }
}