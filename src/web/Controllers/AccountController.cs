using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using core.Account;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
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
        public object Identity()
        {
            var claims = this.User.Claims.Select(
                c => $"{c.Type}:{c.Value}"
            );
            
            var user = new
            {
                username = this.User.Identifier(),
                loggedIn = this.User.Identity.IsAuthenticated,
                claims
            };

            return user;
        }

        [HttpGet()]
        public Task<object> Get()
        {
            var query = new Get.Query(this.User.Identifier());

            return _mediator.Send(query);
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
            if (error != null)
            {
                return GenerateBadRequestWithError(error);
            }

            await EstablishSignedInIdentity(r.User);

            return Ok();
        }

        private async Task EstablishSignedInIdentity(User user)
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

            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        }

        private ActionResult GenerateBadRequestWithError(string error)
        {
            var dict = new Microsoft.AspNetCore.Mvc.ModelBinding.ModelStateDictionary();
            dict.AddModelError("email", error);
            return BadRequest(dict);
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
            if (error != null)
            {
                return GenerateBadRequestWithError(error);
            }

            await EstablishSignedInIdentity(r.User);
            
            return Ok();
        }

        [HttpGet("logout")]
        public async Task<ActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();

            return this.Redirect("~/");
        }

        [HttpPost("delete")]
        public async Task<ActionResult> Delete(Delete.Command cmd)
        {
            cmd.WithUserId(this.User.Identifier());
            
            await _mediator.Send(cmd);

            await HttpContext.SignOutAsync();

            return Ok();
        }
    }
}