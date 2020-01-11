using System;
using System.Linq;
using core.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private IAccountStorage _storage;

        public AccountController(IAccountStorage storage)
        {
            _storage = storage;
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
        public ActionResult Login()
        {
            var entry = new LoginLogEntry(
                this.User.Identifier(),
                DateTime.UtcNow
            );

            this._storage.RecordLogin(entry);
            
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