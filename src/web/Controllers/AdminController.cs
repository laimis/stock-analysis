using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Adapters.Emails;
using core.Admin;
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

        [HttpPost("weekly")]
        public async Task<ActionResult> Weekly(Weekly.Command cmd)
        {
            await _mediator.Send(cmd);

            return Ok();
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
                new Sender(obj.From, obj.FromName),
                obj.Subject,
                obj.Body
            );

            return this.Ok();
        }

        [HttpGet("users")]
        public async Task<ActionResult> ActiveAccountsAsync()
        {
            var users = await _storage.GetUserEmailIdPairs();

            var total = 0;
            var loggedIn = 0;

            var tableHtml = new StringBuilder();

            tableHtml.Append(@"
            <table>
                <tr>
                    <th>Email</th>
                    <th>User Id</th>
                    <th>Last Login</th>
                    <th>Verified</th>
                    <th>Stocks</th>
                    <th>Options</th>
                    <th>Notes</th>
                </tr>");

            foreach(var (email,userId) in users)
            {
                tableHtml.AppendLine($"<tr>");
                tableHtml.Append($"<td>{email}</td>");
                tableHtml.Append($"<td>{userId}</td>");

                var guid = new System.Guid(userId);

                var user = await _storage.GetUser(guid);

                tableHtml.Append($"<td>{user?.LastLogin?.ToString()}</td>");
                tableHtml.Append($"<td>{user?.Verified}</td>");
                
                var options = await _portfolio.GetOwnedOptions(guid);
                var notes = await _portfolio.GetNotes(guid);
                var stocks = await _portfolio.GetStocks(guid);

                tableHtml.Append($"<td>{stocks.Count()}</td>");
                tableHtml.Append($"<td>{options.Count()}</td>");
                tableHtml.Append($"<td>{notes.Count()}</td>");

                tableHtml.AppendLine("</tr>");

                total++;
                if (user != null && user.LastLogin.HasValue)
                {
                    loggedIn++;
                }
            }

            tableHtml.AppendLine("</table>");

            var body = $@"<html>
                <body>
                    <h3>Users: {total}</h3>
                    <h4>Logged In: {loggedIn}</h4>
                    ${tableHtml.ToString()}
                </body>
            </html>";

            return new ContentResult {
                Content = body,
                ContentType = "text/html"
            };
        }
    }
}