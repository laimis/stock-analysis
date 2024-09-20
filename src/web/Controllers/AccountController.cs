using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using core.fs;
using core.fs.Accounts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.FSharp.Core;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController(Handler handler) : ControllerBase
    {
        [HttpGet("status")]
        public Task<ActionResult> Identity()
        {
            if (User.Identity is { IsAuthenticated: false })
            {
                var view = AccountStatusView.notFound();
                return Task.FromResult<ActionResult>(Ok(view));
            }

            return this.OkOrError(
                handler.Handle(
                    new LookupById(User.Identifier())
                )
            );
        }
        

        [HttpPost("validate")]
        public Task<ActionResult> Validate([FromBody]UserInfo command)
        {
            return User.Identity is { IsAuthenticated: true } ?
                Task.FromResult<ActionResult>(BadRequest("User already has an account")) 
                : this.OkOrError(handler.Validate(command));
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody]CreateAccount cmd)
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                return BadRequest("User already has an account");
            }

            var r = await handler.Handle(cmd);
            if (r.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, r.ResultValue);
            }

            return this.OkOrError(r);
        }

        internal static async Task EstablishSignedInIdentity(HttpContext context, AccountStatusView user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.GivenName,             user.Firstname),
                new(ClaimTypes.Surname,               user.Lastname),
                new(ClaimTypes.Email,                 user.Email),
                new(IdentityExtensions.ID_CLAIM_NAME, user.Id.ToString())
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

        [HttpGet("integrations/brokerage/connect")]
        [Authorize]
        public async Task<ActionResult> Brokerage()
        {
            var url = await handler.Handle(new Connect());

            return Redirect(url);
        }

        [HttpGet("integrations/brokerage")]
        [Authorize]
        public Task<ActionResult> BrokerageInfo() =>
            this.OkOrError(
                handler.HandleInfo(
                    new BrokerageInfo(User.Identifier()
                    )
                )
            );
        
        private ActionResult RedirectOrError<T>(FSharpResult<T,ServiceError> result)
        {
            if (result.IsError)
            {
                return BadRequest(result.ErrorValue.Message);
            }
            
            return Redirect("~/profile");
        }

        [HttpGet("integrations/brokerage/disconnect")]
        [Authorize]
        public async Task<ActionResult> BrokerageDisconnect()
        {
            var result = await handler.HandleDisconnect(
                new BrokerageDisconnect(User.Identifier())
            );
            return RedirectOrError(result);
        }

        [HttpGet("integrations/brokerage/callback")]
        [Authorize]
        public async Task<ActionResult> BrokerageCallback([FromQuery]string code)
        {
            var result = await handler.HandleConnectCallback(
                new ConnectCallback(code, User.Identifier()));

            return RedirectOrError(result);
        }

        [HttpPost("requestpasswordreset")]
        public ActionResult RequestPasswordReset([FromBody] RequestPasswordReset cmd)
        {
            handler.Handle(cmd);
            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Authenticate([FromBody]Authenticate cmd)
        {
            var response = await handler.Handle(cmd);
            if (response.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, response.ResultValue);
            }
            
            return this.OkOrError(response);
        }

        [HttpPost("contact")]
        public Task<ActionResult> Contact([FromBody]Contact cmd) => this.OkOrError(handler.Handle(cmd));

        [HttpGet("logout")]
        [Authorize]
        public async Task<ActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();

            return Redirect("~/");
        }

        [HttpPost("delete")]
        [Authorize]
        public async Task<ActionResult> Delete([FromBody]Delete cmd)
        {
            var result = await handler.HandleDelete(User.Identifier(), cmd);
            
            if (result.IsError)
            {
                return this.Error(result.ErrorValue);
            }

            await HttpContext.SignOutAsync();
            return Ok();
        }

        [HttpPost("clear")]
        [Authorize]
        public async Task<ActionResult> Clear()
        {
            await handler.Handle(new Clear(User.Identifier()));

            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpPost("resetpassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPassword cmd)
        {
            var result = await handler.Handle(cmd);

            if (result.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, result.ResultValue);
            }

            return this.OkOrError(result);
        }

        [HttpGet("confirm/{id}")]
        public async Task<ActionResult> Confirm(Guid id)
        {
            var result = await handler.Handle(
                new Confirm(id)
            );

            if (result.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, result.ResultValue);
                return Redirect("~/");
            }
            
            return this.Error(result.ErrorValue);
        }
        
        [HttpPost("settings")]
        [Authorize]
        public Task<ActionResult> Settings([FromBody]SetSetting cmd)
            => this.OkOrError(handler.HandleSettings(User.Identifier(), cmd));
        
        [HttpGet("transactions")]
        public Task<ActionResult> GetTransactions()
        {
            return this.OkOrError(
                handler.Handle(
                    new GetAccountTransactions(User.Identifier())
                )
            );
        }

        [HttpPost("transactions/{transactionId}/applied")]
        public Task<ActionResult> ApplyTransaction(string transactionId)
        {
            return this.OkOrError(
                handler.Handle(
                    new MarkAccountTransactionAsApplied(User.Identifier(), transactionId)
                )
            );
        }
    }
}
