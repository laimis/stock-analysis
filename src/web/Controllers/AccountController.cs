using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using core.fs.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        [HttpGet("status")]
        public Task<ActionResult> Identity([FromServices]Status.Handler handler) =>
            this.OkOrError(handler.Handle(new Status.LookupById(User.Identifier())));
        

        [HttpPost("validate")]
        public async Task<ActionResult> Validate([FromBody]Create.UserInfo command, [FromServices]Create.Handler service)
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                return BadRequest("User already has an account");
            }

            var result = await service.Validate(command);

            return this.OkOrError(result);
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody]Create.Command cmd, [FromServices]Create.Handler service)
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                return BadRequest("User already has an account");
            }

            var r = await service.Handle(cmd);
            var error = r.Error;
            if (error == null)
            {
                await EstablishSignedInIdentity(HttpContext, r.Success);
            }

            return this.OkOrError(r);
        }

        internal static async Task EstablishSignedInIdentity(HttpContext context, User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.GivenName,             user.State.Firstname),
                new Claim(ClaimTypes.Surname,               user.State.Lastname),
                new Claim(ClaimTypes.Email,                 user.State.Email),
                new Claim(IdentityExtensions.ID_CLAIM_NAME, user.State.Id.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var principal = new ClaimsPrincipal(claimsIdentity);

            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        // this seemed to have been used to track users logging in on User aggregate
        // I have changed my mind and won't pollute user aggregate with what looks like
        // infra concerns. I removed the logic that used to create login command and send it
        // to a service for processing. If we need such functionality, we can create login
        // tracking service that's separate from user domain object.
        [HttpGet("login")]
        [Authorize]
        public ActionResult Login() => Redirect("~/");

        [HttpGet("integrations/tdameritrade/connect")]
        [Authorize]
        public async Task<ActionResult> TdAmeritrade([FromServices]Brokerage.Handler service)
        {
            var url = await service.HandleConnect(new Brokerage.Connect());

            return Redirect(url);
        }

        [HttpGet("integrations/tdameritrade")]
        [Authorize]
        public Task<ActionResult> TdAmeritradeInfo([FromServices] Brokerage.Handler service) =>
            this.OkOrError(
                service.HandleInfo(
                    new Brokerage.Info(User.Identifier()
                    )
                )
            );

        [HttpGet("integrations/tdameritrade/disconnect")]
        [Authorize]
        public async Task<ActionResult> TdAmeritradeDisconnect([FromServices] Brokerage.Handler service)
        {
            var result = await service.HandleDisconnect(
                new Brokerage.Disconnect(User.Identifier())
            );

            return result.Error switch {
                null => Redirect("~/profile"),
                _ => BadRequest(result.Error)
            };
        }

        [HttpGet("integrations/tdameritrade/callback")]
        [Authorize]
        public async Task<ActionResult> TdAmeritradeCallback([FromQuery]string code, [FromServices] Brokerage.Handler service)
        {
            var result = await service.HandleConnectCallback(
                new Brokerage.ConnectCallback(code, User.Identifier()));

            return result.Error switch {
                null => Redirect("~/profile"),
                _ => BadRequest(result.Error)
            };
        }

        [HttpPost("requestpasswordreset")]
        public Task<ActionResult> RequestPasswordReset([FromBody]PasswordReset.RequestCommand cmd, [FromServices] PasswordReset.Handler service) =>
            this.OkOrError(service.Handle(cmd));

        [HttpPost("login")]
        public async Task<ActionResult> Authenticate([FromBody]Authenticate.Command cmd, [FromServices]Authenticate.Handler service)
        {
            var response = await service.Handle(cmd);
            if (response.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, response.Success);
            }
            
            return this.OkOrError(response);
        }

        [HttpPost("contact")]
        public Task<ActionResult> Contact([FromBody]Contact.Command cmd, [FromServices]Contact.Handler service)
            => this.OkOrError(service.Handle(cmd));

        [HttpGet("logout")]
        [Authorize]
        public async Task<ActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();

            return Redirect("~/");
        }

        [HttpPost("delete")]
        [Authorize]
        public async Task<ActionResult> Delete([FromBody]DeleteAccount.Command cmd, [FromServices]DeleteAccount.Handler service)
        {
            cmd = cmd.WithUserId(User.Identifier());
            
            var result = await service.Handle(cmd);
            
            if (result.Error != null)
            {
                return this.Error(result.Error.Message);
            }
            else
            {
                await HttpContext.SignOutAsync();
                return Ok();
            }
        }

        [HttpPost("clear")]
        [Authorize]
        public async Task<ActionResult> Clear([FromServices]ClearAccount.Handler handler)
        {
            await handler.Handle(new ClearAccount.Command(User.Identifier()));

            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpPost("resetpassword")]
        public async Task<ActionResult> ResetPassword([FromBody] Create.ResetPassword cmd, [FromServices]Create.Handler handler)
        {
            var result = await handler.Handle(cmd);

            if (result.Error == null)
            {
                await EstablishSignedInIdentity(HttpContext, result.Success);
            }

            return this.OkOrError(result);
        }

        [HttpGet("confirm/{id}")]
        public async Task<ActionResult> Confirm(Guid id, [FromServices] ConfirmAccount.Handler handler)
        {
            var result = await handler.Handle(
                new ConfirmAccount.Command(id)
            );

            if (result.Error != null)
            {
                return this.Error(result.Error.Message);
            }

            await EstablishSignedInIdentity(HttpContext, result.Success);

            return Redirect("~/");
        }
    }
}