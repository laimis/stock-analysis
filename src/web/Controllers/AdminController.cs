using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using core;
using core.Account;
using core.Account.Handlers;
using core.Admin;
using core.Shared.Adapters.Emails;
using core.Shared.Adapters.Storage;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

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

        [HttpGet("test")]
        public ActionResult Test() => Ok();

        [HttpGet("loginas/{userId}")]
        public async Task<ActionResult> LoginAs(Guid userId)
        {
            var u = await _storage.GetUser(userId);

            await AccountController.EstablishSignedInIdentity(HttpContext, u);

            return Redirect("~/");
        }

        [HttpGet("delete/{userId}")]
        public async Task<ActionResult> Delete(Guid userId)
        {
            var cmd = new Delete.Command();
            
            cmd.WithUserId(userId);

            await _mediator.Send(cmd);

            return Ok();
        }

        [HttpPost("email")]
        public async Task<ActionResult> Email(EmailInput obj)
        {
            await _email.Send(
                new Recipient(email: obj.To, name: null),
                new Sender(obj.From, obj.FromName),
                obj.Subject,
                obj.Body
            );

            return Ok();
        }

        [HttpGet("welcome")]
        public async Task<ActionResult> Welcome(Guid userId)
        {
            var user = await _storage.GetUser(userId);

            await _email.Send(
                new Recipient(email: user.State.Email, name: user.State.Name),
                Sender.Support,
                EmailTemplate.NewUserWelcome,
                new object()
            );

            return Ok();
        }

        [HttpGet("users")]
        public Task<object> ActiveAccountsAsync()
        {
            return _mediator.Send(new Users.Query());
        }

        [HttpGet("users/export")]
        public Task<ActionResult> Export()
        {
            return this.GenerateExport(_mediator, new Users.Export());
        }
    }

    public class EmailInput
    {
        [Required]
        public string From { get; set; }
        [Required]
        public string FromName { get; set; }
        [Required]
        public string Subject { get; set; }
        [Required]
        public string Body { get; set; }
        [Required]
        public string To { get; set; }
    }
}