using System;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Admin;
using core.fs.Shared.Adapters.Email;
using core.fs.Shared.Adapters.Storage;
using core.fs.Shared.Domain.Accounts;
using core.Shared.Adapters;
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
        private readonly IAccountStorage _storage;
        private readonly IEmailService _email;
        private readonly IRoleService _roleService;

        public AdminController(
            IAccountStorage storage,
            IEmailService email,
            IRoleService roleService)
        {
            _storage = storage;
            _email = email;
            _roleService = roleService;
        }

        [HttpGet("test")]
        public ActionResult Test() => Ok();

        [HttpGet("loginas/{userId}")]
        public async Task<ActionResult> LoginAs([FromRoute]Guid userId)
        {
            var u = await _storage.GetUser(UserId.NewUserId(userId));
            
            if (u.Value == null)
                return NotFound();

            var view = AccountStatusView.fromUserState(_roleService.IsAdmin(u.Value.State), u.Value.State);

            await AccountController.EstablishSignedInIdentity(HttpContext, view);

            return Redirect("~/");
        }

        [HttpGet("delete/{userId}")]
        public Task<ActionResult> Delete([FromRoute]Guid userId, DeleteAccount.Handler service) =>
            this.OkOrError(service.Handle(new DeleteAccount.Command(UserId.NewUserId(userId), null)));

        [HttpPost("email")]
        public async Task<ActionResult> Email(EmailInput obj)
        {
            await _email.SendWithInput(obj);

            return Ok();
        }

        [HttpGet("welcome")]
        public async Task<ActionResult> Welcome([FromQuery]Guid userId)
        {
            var user = await _storage.GetUser(UserId.NewUserId(userId));
            
            if (user.Value == null)
                return NotFound();

            await _email.SendWithTemplate(
                new Recipient(email: user.Value.State.Email, name: user.Value.State.Name),
                Sender.Support,
                EmailTemplate.NewUserWelcome,
                new object()
            );

            return Ok();
        }

        [HttpGet("users")]
        public Task<ActionResult> ActiveAccountsAsync([FromServices]Users.Handler service)
        {
            return this.OkOrError(
                service.Handle(
                    new Users.Query(true)
                )
            );
        }

        [HttpGet("users/export")]
        public Task<ActionResult> Export([FromServices]Users.Handler service)
        {
            return this.GenerateExport(
                service.Handle(new Users.Export())
            );
        }
    }
}