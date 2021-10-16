using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using web.Utils;

namespace web.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private ILogger<AccountController> _logger;
        private IMediator _mediator;

        public AccountController(ILogger<AccountController> logger, IMediator mediator)
        {
            _logger = logger;
            _mediator = mediator;
        }
        
        [HttpGet("status")]
        public Task<object> IdentityAsync()
        {
            return _mediator.Send(new Status.Query(User.Identifier()));
        }

        [HttpPost("validate")]
        public async Task<ActionResult> Validate(Validate.Command cmd)
        {
            if (User.Identity.IsAuthenticated)
            {
                return BadRequest("User already has an account");
            }

            var r = await _mediator.Send(cmd);

            return this.OkOrError(r);
        }

        [HttpPost]
        public async Task<ActionResult> Create(Create.Command cmd)
        {
            if (User.Identity.IsAuthenticated)
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
                User.Identifier(),
                Request.HttpContext.Connection.RemoteIpAddress.ToString());

            var result = await _mediator.Send(cmd);

            if (result != "")
            {
                _logger.LogError("Unable to login: " + result);

                return BadRequest(result);
            }
            
            return Redirect("~/");
        }

        [HttpPost("requestpasswordreset")]
        public async Task<ActionResult> RequestPasswordReset(PasswordReset.Request cmd)
        {
            cmd.WithIPAddress(
                Request.HttpContext.Connection.RemoteIpAddress.ToString()
            );

            var r = await _mediator.Send(cmd);

            return Ok();
        }

        [HttpPost("login")]
        public async Task<ActionResult> Authenticate(Authenticate.Command cmd)
        {
            cmd.WithIPAddress(
                Request.HttpContext.Connection.RemoteIpAddress.ToString()
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

            return Redirect("~/");
        }

        [HttpPost("delete")]
        [Authorize]
        public async Task<ActionResult> Delete(Delete.Command cmd)
        {
            cmd.WithUserId(User.Identifier());
            
            await _mediator.Send(cmd);

            await HttpContext.SignOutAsync();

            return Ok();
        }

        [HttpPost("clear")]
        [Authorize]
        public async Task<ActionResult> Clear(Clear.Command cmd)
        {
            cmd.WithUserId(User.Identifier());
            
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