using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using core.Adapters.Subscriptions;
using MediatR;
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
        private IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }
        
        [HttpGet("status")]
        public Task<object> IdentityAsync()
        {
            return _mediator.Send(new Get.Query(this.User.Identifier()));
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<ActionResult> Subscribe(Subscribe.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpPost("validate")]
        public async Task<ActionResult> Validate(Validate.Command cmd)
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return BadRequest("User already has an account");
            }

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Create.Command cmd)
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return BadRequest("User already has an account");
            }

            var r = await _mediator.Send(cmd);

            var error = r.Error;
            if (error == null)
            {
                await EstablishSignedInIdentity(HttpContext, r.Aggregate);
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

        [HttpGet("login")]
        [Authorize]
        public async Task<ActionResult> LoginAsync()
        {
            var cmd = new Login.Command(
                this.User.Identifier(),
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString());

            await _mediator.Send(cmd);
            
            return this.Redirect("~/");
        }

        [HttpPost("requestpasswordreset")]
        public async Task<ActionResult> RequestPasswordReset(PasswordReset.Request cmd)
        {
            cmd.WithIPAddress(
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString()
            );

            var r = await _mediator.Send(cmd);

            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Authenticate(Authenticate.Command cmd)
        {
            if (this.User.Identity.IsAuthenticated)
            {
                return BadRequest("User is already authenticated");
            }

            cmd.WithIPAddress(
                this.Request.HttpContext.Connection.RemoteIpAddress.ToString()
            );

            var r = await _mediator.Send(cmd);

            var error = r.Error;
            if (error == null)
            {
                await EstablishSignedInIdentity(HttpContext, r.Aggregate);
            }
            
            return this.OkOrError(r);
        }

        [HttpPost("contact")]
        public async Task<ActionResult> Contact(Contact.Command cmd)
        {
            var r = await _mediator.Send(cmd);

            return Ok();
        }

        [HttpGet("logout")]
        [Authorize]
        public async Task<ActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();

            return this.Redirect("~/");
        }

        [HttpPost("delete")]
        [Authorize]
        public async Task<ActionResult> Delete(Delete.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());
            
            await _mediator.Send(cmd);

            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpPost("resetpassword")]
        public async Task<ActionResult> ResetPassword(ResetPassword.Command cmd)
        {
            var r = await _mediator.Send(cmd);

            var error = r.Error;
            if (error == null)
            {
                await EstablishSignedInIdentity(HttpContext, r.Aggregate);
            }

            return this.OkOrError(r);
        }

        [HttpGet("confirm/{id}")]
        public async Task<ActionResult> Confirm(Guid id)
        {
            var cmd = new Confirm.Command(id);

            var r = await _mediator.Send(cmd);

            var error = r.Error;
            if (error != null)
            {
                return this.Error(error);
            }

            await EstablishSignedInIdentity(HttpContext, r.Aggregate);

            return Redirect("~/");
        }
    }
}