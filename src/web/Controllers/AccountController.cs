using core.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using web.Utils;

namespace web.Controllers
{
    [Route("api/[controller]")]
    public class AccountController : Controller
    {
        private IAccountStorage _storage;

        public AccountController(IAccountStorage storage)
        {
            _storage = storage;
        }
        
        [HttpGet("status")]
        public object Identity()
        {
            var user = new
            {
                username = this.User.Identifier(),
                loggedIn = this.User.Identity.IsAuthenticated
            };

            return user;
        }

        [HttpGet("login")]
        [Authorize]
        public ActionResult Login()
        {
            this._storage.RecordLogin(this.User.Identifier());
            
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