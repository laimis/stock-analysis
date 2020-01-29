using System;
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

        [HttpGet("login")]
        [Authorize]
        public async Task<ActionResult> LoginAsync()
        {
            await _mediator.Send(new Login.Command(this.User.Identifier()));
            
            return this.Redirect("~/");
        }

        [HttpGet("logout")]
        public async System.Threading.Tasks.Task<ActionResult> LogoutAsync()
        {
            await HttpContext.SignOutAsync();

            return new ContentResult
            {
                Content = "You have been signed out."
            };
        }
    }
}