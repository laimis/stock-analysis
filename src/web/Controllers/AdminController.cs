using System;
using System.Threading.Tasks;
using core.fs.Accounts;
using core.fs.Adapters.Email;
using core.fs.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;
using Handler = core.fs.Admin.Handler;

namespace web.Controllers
{
    [ApiController]
    [Authorize("admin")]
    [Route("api/[controller]")]
    public class AdminController(Handler handler) : ControllerBase
    {
        [HttpGet("test")]
        public ActionResult Test() => Ok();

        [HttpGet("loginas/{userId}")]
        public async Task<ActionResult> LoginAs([FromRoute]Guid userId, [FromServices]core.fs.Accounts.Handler handler)
        {
            var status = await handler.Handle(new LookupById(UserId.NewUserId(userId)));

            if (status.IsError)
            {
                return NotFound();
            }
            
            await AccountController.EstablishSignedInIdentity(HttpContext, status.ResultValue);

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
                handler.Handle(
                    new SendEmail(
                        obj
                    )
                )
            );
        

        [HttpGet("welcome")]
        public Task<ActionResult> Welcome([FromQuery]Guid userId) =>
            this.OkOrError(
                handler.Handle(
                    new SendWelcomeEmail(UserId.NewUserId(userId))
                )
            );

        [HttpGet("users")]
        public Task<ActionResult> ActiveAccounts()
        {
            return this.OkOrError(
                handler.Handle(
                    new Query(true)
                )
            );
        }

        [HttpGet("users/export")]
        public async Task<ActionResult> Export() => this.GenerateExport(
            await handler.Handle(
                new Export()
            )
        );
    }
}
