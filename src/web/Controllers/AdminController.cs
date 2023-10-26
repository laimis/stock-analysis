using System;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Admin;
using core.fs.Shared.Adapters.Email;
using core.fs.Shared.Domain.Accounts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using Handler = core.fs.Admin.Handler;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly Handler _handler;

        public AdminController(
            Handler handler)
        {
            _handler = handler;
        }

        [HttpGet("test")]
        public ActionResult Test() => Ok();

        [HttpGet("loginas/{userId}")]
        public async Task<ActionResult> LoginAs([FromRoute]Guid userId, [FromServices]core.fs.Accounts.Handler handler)
        {
            var status = await handler.Handle(new LookupById(UserId.NewUserId(userId)));

            if (status.IsOk == false)
            {
                return NotFound();
            }
            
            await AccountController.EstablishSignedInIdentity(HttpContext, status.Success.Value);

            return Redirect("~/");
        }

        [HttpGet("delete/{userId}")]
        public Task<ActionResult> Delete([FromRoute] Guid userId, [FromServices] core.fs.Accounts.Handler service) =>
            this.OkOrError(
                service.HandleDelete(
                    UserId.NewUserId(userId),
                    new Delete(null)
                )
            );

        [HttpPost("email")]
        public Task<ActionResult> Email(EmailInput obj) =>
            this.OkOrError(
                _handler.Handle(
                    new SendEmail(
                        obj
                    )
                )
            );
        

        [HttpGet("welcome")]
        public Task<ActionResult> Welcome([FromQuery]Guid userId) =>
            this.OkOrError(
                _handler.Handle(
                    new SendWelcomeEmail(UserId.NewUserId(userId))
                )
            );

        [HttpGet("users")]
        public Task<ActionResult> ActiveAccounts()
        {
            return this.OkOrError(
                _handler.Handle(
                    new Query(true)
                )
            );
        }

        [HttpGet("users/export")]
        public Task<ActionResult> Export() => this.GenerateExport(
            _handler.Handle(
                new Export()
            )
        );
    }
}