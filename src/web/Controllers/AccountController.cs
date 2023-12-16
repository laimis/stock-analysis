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
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly Handler _handler;
        
        public AccountController(Handler handler) => _handler = handler;
        
        [HttpGet("status")]
        public Task<ActionResult> Identity()
        {
            if (User.Identity is { IsAuthenticated: false })
            {
                var view = AccountStatusView.notFound();
                return Task.FromResult<ActionResult>(Ok(view));
            }

            return this.OkOrError(
                _handler.Handle(
                    new LookupById(User.Identifier())
                )
            );
        }
        

        [HttpPost("validate")]
        public Task<ActionResult> Validate([FromBody]UserInfo command)
        {
            return User.Identity is { IsAuthenticated: true } ?
                Task.FromResult<ActionResult>(BadRequest("User already has an account")) 
                : this.OkOrError(_handler.Validate(command));
        }

        [HttpPost]
        public async Task<ActionResult> Create([FromBody]CreateAccount cmd)
        {
            if (User.Identity is { IsAuthenticated: true })
            {
                return BadRequest("User already has an account");
            }

            var r = await _handler.Handle(cmd);
            if (r.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, r.Success.Value);
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

        [HttpGet("integrations/tdameritrade/connect")]
        [Authorize]
        public async Task<ActionResult> TdAmeritrade()
        {
            var url = await _handler.Handle(new Connect());

            return Redirect(url);
        }

        [HttpGet("integrations/tdameritrade")]
        [Authorize]
        public Task<ActionResult> TdAmeritradeInfo() =>
            this.OkOrError(
                _handler.HandleInfo(
                    new BrokerageInfo(User.Identifier()
                    )
                )
            );
        
        private ActionResult RedirectOrError(ServiceResponse result)
        {
            if (result.IsError)
            {
                var error = result as ServiceResponse.Error;
                return BadRequest(error!.Item.Message);
            }
            
            return Redirect("~/profile");
        }

        [HttpGet("integrations/tdameritrade/disconnect")]
        [Authorize]
        public async Task<ActionResult> TdAmeritradeDisconnect()
        {
            var result = await _handler.HandleDisconnect(
                new BrokerageDisconnect(User.Identifier())
            );
            return RedirectOrError(result);
        }

        [HttpGet("integrations/tdameritrade/callback")]
        [Authorize]
        public async Task<ActionResult> TdAmeritradeCallback([FromQuery]string code)
        {
            var result = await _handler.HandleConnectCallback(
                new ConnectCallback(code, User.Identifier()));

            return RedirectOrError(result);
        }

        [HttpPost("requestpasswordreset")]
        public Task<ActionResult> RequestPasswordReset([FromBody]RequestPasswordReset cmd) => this.OkOrError(_handler.Handle(cmd));

        [HttpPost("login")]
        public async Task<ActionResult> Authenticate([FromBody]Authenticate cmd)
        {
            var response = await _handler.Handle(cmd);
            if (response.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, response.Success.Value);
            }
            
            return this.OkOrError(response);
        }

        [HttpPost("contact")]
        public Task<ActionResult> Contact([FromBody]Contact cmd) => this.OkOrError(_handler.Handle(cmd));

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
            var result = await _handler.HandleDelete(User.Identifier(), cmd);
            
            if (result.IsError)
            {
                var error = result as ServiceResponse.Error;
                return this.Error(error!.Item.Message);
            }

            await HttpContext.SignOutAsync();
            return Ok();
        }

        [HttpPost("clear")]
        [Authorize]
        public async Task<ActionResult> Clear()
        {
            await _handler.Handle(new Clear(User.Identifier()));

            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpPost("resetpassword")]
        public async Task<ActionResult> ResetPassword([FromBody] ResetPassword cmd)
        {
            var result = await _handler.Handle(cmd);

            if (result.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, result.Success.Value);
            }

            return this.OkOrError(result);
        }

        [HttpGet("confirm/{id}")]
        public async Task<ActionResult> Confirm(Guid id)
        {
            var result = await _handler.Handle(
                new Confirm(id)
            );

            if (result.IsOk)
            {
                await EstablishSignedInIdentity(HttpContext, result.Success.Value);
                return Redirect("~/");
            }
            
            return this.Error(result.Error.Value.Message);
        }
        
        [HttpPost("settings")]
        [Authorize]
        public Task<ActionResult> Settings([FromBody]SetSetting cmd)
            => this.OkOrError(_handler.HandleSettings(User.Identifier(), cmd));
    }
}